using System;
using System.Collections.Generic;
using HugsLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/* 
	 * A self-replicating Thing with a concentration property.
	 * Will spread in cardinal directions when the concentration is high enough, and loose concentration over time.
	 * See MoteProperties_GasCloud for settings.
	 */
	public class GasCloud : Thing {
		private const float AlphaEasingDivider = 10f;
		private const float SpreadingAnimationDuration = 1f;
		private const float DisplacingConcentrationFraction = .33f;

		public delegate bool TraversibilityTest(Building b, GasCloud g);
		// this can be used to inject open/closed behavior for
		public static readonly Dictionary<Type, TraversibilityTest> TraversibleBuildings = new Dictionary<Type, TraversibilityTest> {
			{typeof(Building_Vent), (d,g)=> true },
			{typeof(Building_Door), (d,g)=> ((Building_Door)d).Open }
		};

		private static int GlobalOffsetCounter;
		private static readonly List<GasCloud> adjacentBuffer = new List<GasCloud>(4);
		private static readonly List<IntVec3> positionBuffer = new List<IntVec3>(4);

		public Vector2 spriteOffset;
		public Vector2 spriteScaleMultiplier = new Vector2(1f, 1f);
		public float spriteAlpha = 1f;
		public float spriteRotation;
		public int relativeZOrder; // to avoid z fighting among clouds
		private MoteProperties_GasCloud gasProps;
		private string cachedMouseoverLabel;
		private float mouseoverLabelCacheTime;
		private bool needsInitialFill;
		
		private float interpolatedAlpha;
		private readonly ValueInterpolator interpolatedOffsetX;
		private readonly ValueInterpolator interpolatedOffsetY;
		private readonly ValueInterpolator interpolatedScale;
		private readonly ValueInterpolator interpolatedRotation;

		//saved fields
		private float concentration;
		protected int gasTicksProcessed;
		//

		public float Concentration {
			get { return concentration; }
		}

		public bool IsBlocked {
			get {
				return !TileIsGasTraversible(Position, Map, this);
			}
		}

		public GasCloud() {
			interpolatedOffsetX = new ValueInterpolator();
			interpolatedOffsetY = new ValueInterpolator();
			interpolatedScale = new ValueInterpolator();
			interpolatedRotation = new ValueInterpolator();
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			gasProps = def.mote as MoteProperties_GasCloud;
			relativeZOrder = ++GlobalOffsetCounter % 80;
			if (gasProps == null) throw new Exception("Missing required gas mote properties in " + def.defName);
			interpolatedScale.value = GetRandomGasScale();
			interpolatedRotation.value = GetRandomGasRotation();
			// uniformly distribute gas ticks to reduce per frame workload
			// wait for next tick to avoid registering while DistributedTickScheduler is mid-tick
			HugsLibController.Instance.TickDelayScheduler.ScheduleCallback(() =>
				HugsLibController.Instance.DistributedTicker.RegisterTickability(GasTick, gasProps.GastickInterval, this)
			, 1, this);
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref concentration, "concentration");
			Scribe_Values.Look(ref gasTicksProcessed, "ticks");
		}

		public override void Draw() {
			if (!Find.TickManager.Paused) {
				UpdateInterpolatedValues();
			}
			var targetAlpha = Mathf.Min(1f, concentration / gasProps.FullAlphaConcentration);
			spriteAlpha = interpolatedAlpha = DoAdditiveEasing(interpolatedAlpha, targetAlpha, AlphaEasingDivider, Time.deltaTime);
			spriteOffset = new Vector2(interpolatedOffsetX, interpolatedOffsetY);
			spriteScaleMultiplier = new Vector2(interpolatedScale, interpolatedScale);
			spriteRotation = interpolatedRotation;
			base.Draw();
		}

		private void UpdateInterpolatedValues() {
			interpolatedOffsetX.Update();
			interpolatedOffsetY.Update();
			if (gasProps.AnimationAmplitude > 0) {
				interpolatedScale.Update();
				interpolatedRotation.Update();
				if (interpolatedOffsetX.finished) {
					// start offset interpolation
					var newX = Rand.Range(-gasProps.AnimationAmplitude, gasProps.AnimationAmplitude);
					var newY = Rand.Range(-gasProps.AnimationAmplitude, gasProps.AnimationAmplitude);
					var duration = gasProps.AnimationPeriod.RandomInRange;
					interpolatedOffsetX.StartInterpolation(newX, duration, CurveType.CubicInOut);
					interpolatedOffsetY.StartInterpolation(newY, duration, CurveType.CubicInOut);
				}
				if (interpolatedScale.finished) {
					// start scale interpolation
					interpolatedScale.StartInterpolation(GetRandomGasScale(), gasProps.AnimationPeriod.RandomInRange, CurveType.CubicInOut);
				}
				if (interpolatedRotation.finished) {
					// start rotation interpolation
					const float MaxRotationDelta = 90f;
					var newRotation = interpolatedRotation.value + Rand.Range(-MaxRotationDelta, MaxRotationDelta)*gasProps.AnimationAmplitude;
					interpolatedRotation.StartInterpolation(newRotation, gasProps.AnimationPeriod.RandomInRange, CurveType.CubicInOut);
				}
			}
		}

		public override string LabelMouseover {
			get {
				if (cachedMouseoverLabel == null || mouseoverLabelCacheTime < Time.realtimeSinceStartup - .5f) {
					var effectivenessPercent = Mathf.Clamp01(Concentration / gasProps.FullAlphaConcentration) * 100f;
					if (concentration >= 1000) {
						cachedMouseoverLabel = "GasCloud_statusReadout_high".Translate(LabelCap, concentration / 1000, effectivenessPercent);
					} else {
						cachedMouseoverLabel = "GasCloud_statusReadout_low".Translate(LabelCap, concentration, effectivenessPercent);
					}
					mouseoverLabelCacheTime = Time.realtimeSinceStartup;
				}
				return cachedMouseoverLabel;
			}
		}

		public void ReceiveConcentration(float amount) {
			concentration += amount;
			if (concentration < 0) concentration = 0;
		}

		public void BeginSpreadingTransition(GasCloud parentCloud, IntVec3 targetPosition) {
			interpolatedOffsetX.value = parentCloud.Position.x - targetPosition.x;
			interpolatedOffsetY.value = parentCloud.Position.z - targetPosition.z;
			interpolatedOffsetX.StartInterpolation(0, SpreadingAnimationDuration, CurveType.QuinticOut);
			interpolatedOffsetY.StartInterpolation(0, SpreadingAnimationDuration, CurveType.QuinticOut);
		}

		protected virtual void GasTick() {
			if (!Spawned) return;
			gasTicksProcessed++;
			// dissipate
			var underRoof = Map.roofGrid.Roofed(Position);
			concentration -= underRoof ? gasProps.RoofedDissipation : gasProps.UnroofedDissipation;
			if(concentration<=0) {
				Destroy(DestroyMode.KillFinalize);
				return;
			}
			
			//spread
			var gasTickFitForSpreading = gasTicksProcessed % gasProps.SpreadInterval == 0;
			if(gasTickFitForSpreading) {
				TryCreateNewNeighbors();
			}

			// if filled in
			if(IsBlocked) {
				ForcePushConcentrationToNeighbors();
			}

			// balance concentration
			ShareConcentrationWithMinorNeighbors();
		}

		private float GetRandomGasScale() {
			return 1f + Rand.Range(-gasProps.AnimationAmplitude, gasProps.AnimationAmplitude);
		}

		private float GetRandomGasRotation() {
			return Rand.Value * 360f;
		}

		// this is just a "current + difference / divider", but adjusted for frame rate
		private float DoAdditiveEasing(float currentValue, float targetValue, float easingDivider, float frameDeltaTime) {
			const float nominalFrameRate = 60f;
			var dividerMultiplier = frameDeltaTime == 0 ? 0 : (1f / nominalFrameRate) / frameDeltaTime;
			easingDivider *= dividerMultiplier;
			if (easingDivider < 1) easingDivider = 1;
			var easingStep = (targetValue - currentValue) / easingDivider;
			return currentValue + easingStep;
		}

		private List<IntVec3> GetSpreadableAdjacentCells() {
			positionBuffer.Clear();
			for (int i = 0; i < 4; i++) {
				var adjPosition = GenAdj.CardinalDirections[i] + Position;
				if (TileIsGasTraversible(adjPosition, Map, this)) {
					var neighborThings = Map.thingGrid.ThingsListAtFast(adjPosition);
					var anyPreventingClouds = false;
					for (int j = 0; j < neighborThings.Count; j++) {
						var cloud = neighborThings[j] as GasCloud;
						// check if a cloud of same type already exists or another type of cloud is too concentrated to expand into
						if (cloud != null && (cloud.def == def || cloud.concentration > concentration * DisplacingConcentrationFraction)) {
							anyPreventingClouds = true;
						}
					}
					if (!anyPreventingClouds) {
						positionBuffer.Add(adjPosition);	
					}
				}
			}
			positionBuffer.Shuffle();
			return positionBuffer;
		}

		private List<GasCloud> GetAdjacentGasCloudsOfSameDef() {
			adjacentBuffer.Clear();
			for (int i = 0; i < 4; i++) {
				var adjPosition = GenAdj.CardinalDirections[i] + Position;
				if (!adjPosition.InBounds(Map)) continue;
				var cloud = adjPosition.GetFirstThing(Map, def) as GasCloud;
				if(cloud != null) {
					adjacentBuffer.Add(cloud);
				}
			}
			return adjacentBuffer;
		}

		private void ShareConcentrationWithMinorNeighbors() {
			var neighbors = GetAdjacentGasCloudsOfSameDef();
			var numSharingNeighbors = 0;
			for (int i = 0; i < neighbors.Count; i++) {
				var neighbor = neighbors[i];
				// do not push to a blocked cloud, unless it's one we created this tick
				if (neighbor.Concentration >= concentration || (!neighbor.needsInitialFill && neighbor.IsBlocked)) {
					neighbors[i] = null;
				} else {
					numSharingNeighbors++;
				}
			}
			if (numSharingNeighbors > 0) {
				for (int i = 0; i < neighbors.Count; i++) {
					var neighbor = neighbors[i];
					if (neighbor == null) continue;
					var neighborConcentration = neighbor.concentration > 0 ? neighbor.Concentration : 1;
					var amountToShare = ((concentration - neighborConcentration)/(numSharingNeighbors+1))*gasProps.SpreadAmountMultiplier;
					neighbor.ReceiveConcentration(amountToShare);
					neighbor.needsInitialFill = false;
					concentration -= amountToShare;
				}

			}
		}

		private void ForcePushConcentrationToNeighbors() {
			var neighbors = GetAdjacentGasCloudsOfSameDef();
			for (int i = 0; i < neighbors.Count; i++) {
				var neighbor = neighbors[i];
				if (neighbor.IsBlocked) continue;
				var pushAmount = concentration/neighbors.Count;
				neighbor.ReceiveConcentration(pushAmount);
				concentration -= pushAmount;
			}
		}

		private void TryCreateNewNeighbors() {
			var spreadsLeft = Mathf.FloorToInt(concentration / gasProps.SpreadMinConcentration);
			if (spreadsLeft <= 0) return;
			var viableCells = GetSpreadableAdjacentCells();
			for (int i = 0; i < viableCells.Count; i++) {
				if (spreadsLeft <= 0) break;
				var targetPosition = viableCells[i];
				// place on next Normal tick. We cannot register while DistributedTickScheduler is ticking
				var newCloud = (GasCloud)ThingMaker.MakeThing(def);
				newCloud.needsInitialFill = true;
				newCloud.BeginSpreadingTransition(this, targetPosition);	
				GenPlace.TryPlaceThing(newCloud, targetPosition, Map, ThingPlaceMode.Direct);
				spreadsLeft--;
			}
		}

		private bool TileIsGasTraversible(IntVec3 pos, Map map, GasCloud sourceCloud) {
			if (!pos.InBounds(map) || !map.pathGrid.WalkableFast(pos)) return false;
			var thingList = map.thingGrid.ThingsListAtFast(pos);
			for (var i = 0; i < thingList.Count; i++) {
				var thing = thingList[i];
				// check for conditionally traversable buildings
				var building = thing as Building;
				if (building != null) {
					TraversibilityTest travTest;
					TraversibleBuildings.TryGetValue(building.GetType(), out travTest);
					if (travTest != null && !travTest(building, sourceCloud)) {
						return false;
					}
				}
				// check for more concentrated gases of a different def
				var cloud = thing as GasCloud;
				if (cloud != null && cloud.def != sourceCloud.def && sourceCloud.concentration < cloud.concentration) {
					return false;
				}
			}
			return true;
		}
	}
}
