using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	public class GasCloud : Thing {
		private const float AlphaEasingDivider = 10f;
		private const float SpreadingAnimationDuration = 1f;

		private const string ConcentrationLabelId = "GasCloud_concentration_label";
		// uniformely distribute gas ticks to reduce per frame workload
		private static int GlobalTickOffsetCounter;

		public Vector2 spriteOffset;
		public Vector2 spriteScaleMultiplier = new Vector2(1f, 1f);
		public float spriteAlpha = 1f;
		public float spriteRotation;

		private readonly List<GasCloud> adjacentBuffer = new List<GasCloud>(4);
		private readonly List<IntVec3> positionBuffer = new List<IntVec3>(4);
		private MoteProperties_GasCloud gasProps;
		
		private float interpolatedAlpha;
		private DisposablePrimitiveWrapper<float> interpolatedOffsetX;
		private DisposablePrimitiveWrapper<float> interpolatedOffsetY;
		private DisposablePrimitiveWrapper<float> interpolatedScale;
		private DisposablePrimitiveWrapper<float> interpolatedRotation;

		//saved fields
		private int gasTickOffset;
		private float concentration;
		//

		public float Concentration {
			get { return concentration; }
		}

		public override void SpawnSetup() {
			base.SpawnSetup();
			gasProps = def.mote as MoteProperties_GasCloud;
			gasTickOffset = ++GlobalTickOffsetCounter;
			if (gasProps == null) throw new Exception("Missing required gas mote properties in " + def.defName);
			var spreadingTransitionStarted = interpolatedOffsetX != null;
			if (!spreadingTransitionStarted) {
				interpolatedOffsetX = new DisposablePrimitiveWrapper<float>(0);
				interpolatedOffsetY = new DisposablePrimitiveWrapper<float>(0);
			}
			interpolatedScale = new DisposablePrimitiveWrapper<float>(GetRandomGasScale());
			interpolatedRotation = new DisposablePrimitiveWrapper<float>(GetRandomGasRotation());
			if (gasProps.AnimationAmplitude > 0) {
				if(!spreadingTransitionStarted) BeginOffsetInterpolation();
				BeginScaleInterpolation();
				BeginRotationInterpolation();
			}
		}

		public override void Draw() {
			var targetApha = Mathf.Min(1f, concentration / gasProps.FullAlphaConcentration);
			spriteAlpha = interpolatedAlpha = DoAdditiveEasing(interpolatedAlpha, targetApha, AlphaEasingDivider, Time.deltaTime);
			spriteOffset = new Vector2(interpolatedOffsetX, interpolatedOffsetY);
			spriteScaleMultiplier = new Vector2(interpolatedScale, interpolatedScale);
			spriteRotation = interpolatedRotation;
			base.Draw();
		}

		public override string GetInspectString() {
			var extra = "adjacent: " + adjacentBuffer.Count + " positions:" + positionBuffer.Count+"\n";
			return extra+string.Format(ConcentrationLabelId.Translate(), string.Format("{0:n0}", concentration));
		}

		public override void Tick() {
			var currentTick = Find.TickManager.TicksGame;
			var tickIsGasTick = (currentTick + gasTickOffset) % gasProps.GastickInterval == 0;
			if (tickIsGasTick) GasTick();
		}

		public void ReceiveConcentration(float amount) {
			concentration += amount;
			if (concentration < 0) concentration = 0;
		}

		public void BeginSpreadingTransition(GasCloud parentCloud, IntVec3 targetPosition) {
			interpolatedOffsetX = new DisposablePrimitiveWrapper<float>(parentCloud.Position.x-targetPosition.x);
			interpolatedOffsetY = new DisposablePrimitiveWrapper<float>(parentCloud.Position.z-targetPosition.z);
			ValueInterpolator.Instance.InterpolateValue(interpolatedOffsetX, 0, SpreadingAnimationDuration, ValueInterpolator.InterpolationCurveType.QuinticEaseOut, OnSpreadingTransitionFinished);
			ValueInterpolator.Instance.InterpolateValue(interpolatedOffsetY, 0, SpreadingAnimationDuration, ValueInterpolator.InterpolationCurveType.QuinticEaseOut);
		}

		public void GasTick() {
			// dissipate
			var underRoof = Find.RoofGrid.Roofed(Position);
			concentration -= underRoof ? gasProps.RoofedDissipation : gasProps.UnroofedDissipation;
			if(concentration<=0) {
				Destroy(DestroyMode.Kill);
				return;
			}
			
			//spread
			var currentTick = Find.TickManager.TicksGame;
			var gasTickFitForSpreading = (currentTick + gasTickOffset) % (gasProps.GastickInterval*gasProps.SpreadInterval) == 0;
			if(gasTickFitForSpreading) {
				TryCreateNewNeighbours();
			}

			// balance concentration
			ShareConcentrationWithMinorNeighbours();
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
			base.Destroy(mode);
			interpolatedOffsetX.Dispose();
			interpolatedOffsetY.Dispose();
			interpolatedScale.Dispose();
			interpolatedRotation.Dispose();
		}

		private void BeginOffsetInterpolation() {
			if(!Spawned) return;
			var newX = Rand.Range(-gasProps.AnimationAmplitude, gasProps.AnimationAmplitude);
			var newY = Rand.Range(-gasProps.AnimationAmplitude, gasProps.AnimationAmplitude);
			ValueInterpolator.Instance.InterpolateValue(interpolatedOffsetX, newX, gasProps.AnimationPeriod.RandomInRange, ValueInterpolator.InterpolationCurveType.CubicEaseInOut, BeginOffsetInterpolation);
			ValueInterpolator.Instance.InterpolateValue(interpolatedOffsetY, newY, gasProps.AnimationPeriod.RandomInRange, ValueInterpolator.InterpolationCurveType.CubicEaseInOut);
		}

		private void BeginScaleInterpolation() {
			if (!Spawned) return;
			var newScale = GetRandomGasScale();
			ValueInterpolator.Instance.InterpolateValue(interpolatedScale, newScale, gasProps.AnimationPeriod.RandomInRange, ValueInterpolator.InterpolationCurveType.CubicEaseInOut, BeginScaleInterpolation);
		}

		private void BeginRotationInterpolation() {
			const float MaxRotationDelta = 90f;
			if (!Spawned) return;
			var newRotation = interpolatedRotation.Value + Rand.Range(-MaxRotationDelta, MaxRotationDelta) * gasProps.AnimationAmplitude;
			ValueInterpolator.Instance.InterpolateValue(interpolatedRotation, newRotation, gasProps.AnimationPeriod.RandomInRange, ValueInterpolator.InterpolationCurveType.CubicEaseInOut, BeginRotationInterpolation);
		}

		private void OnSpreadingTransitionFinished() {
			BeginOffsetInterpolation();
		}

		private float GetRandomGasScale() {
			return 1f + Rand.Range(-gasProps.AnimationAmplitude, gasProps.AnimationAmplitude);
		}

		private float GetRandomGasRotation() {
			return Rand.Value * 360f;
		}

		// this is just a "current + difference / divider", but adjusted for frame rate
		private float DoAdditiveEasing(float currentValue, float targetValue, float easingDivider, float frameDeltaTime) {
			const float nominalFramerate = 60f;
			var dividerMultiplier = frameDeltaTime == 0 ? 0 : (1f / nominalFramerate) / frameDeltaTime;
			easingDivider *= dividerMultiplier;
			if (easingDivider < 1) easingDivider = 1;
			var easingStep = (targetValue - currentValue) / easingDivider;
			return currentValue + easingStep;
		}

		private List<IntVec3> GetSpreadableAdacentCells() {
			positionBuffer.Clear();
			for (int i = 0; i < 4; i++) {
				var adjPosition = GenAdj.CardinalDirections[i] + Position;
				if(Find.PathGrid.Walkable(adjPosition) && Find.ThingGrid.ThingAt<GasCloud>(adjPosition) == null) {
					positionBuffer.Add(adjPosition);
				}
			}
			positionBuffer.Shuffle();
			return positionBuffer;
		}

		private List<GasCloud> GetAdjacentGasClouds() {
			adjacentBuffer.Clear();
			for (int i = 0; i < 4; i++) {
				var adjPosition = GenAdj.CardinalDirections[i] + Position;
				var cloud = Find.ThingGrid.ThingAt<GasCloud>(adjPosition);
				if(cloud!=null) {
					adjacentBuffer.Add(cloud);
				}
			}
			return adjacentBuffer;
		}

		private void ShareConcentrationWithMinorNeighbours() {
			var neighbours = GetAdjacentGasClouds();
			var neighbourConcentrationPool = 0f;
			var numSharingNeighbours = 0;
			var pathGrid = Find.PathGrid;
			for (int i = 0; i < neighbours.Count; i++) {
				var neighbour = neighbours[i];
				if (neighbour.Concentration >= concentration || !pathGrid.WalkableFast(neighbour.Position)) { // also skip clouds on closed off tiles
					neighbours[i] = null;
				} else {
					neighbourConcentrationPool += neighbour.Concentration;
					numSharingNeighbours++;
				}
			}
			if (neighbourConcentrationPool == 0) neighbourConcentrationPool = numSharingNeighbours;
			if (numSharingNeighbours > 0) {
				var includeSelf = pathGrid.WalkableFast(Position) ? 1 : 0; // attempt to push all concentration if own tile became filled
				var equalSharePerNeighbour = (neighbourConcentrationPool + concentration) / (numSharingNeighbours + includeSelf);
				var bestAmountToShare = concentration - equalSharePerNeighbour;
				if (bestAmountToShare > 0) {
					var adjustedAmountToShare = bestAmountToShare * gasProps.SpreadAmountMultiplier;
					for (int i = 0; i < neighbours.Count; i++) {
						var neighbour = neighbours[i];
						if (neighbour == null) continue;
						var neighbourConcentration = neighbour.Concentration > 0 ? neighbour.Concentration : 1;
						var proportionalAmountToShare = (neighbourConcentration / neighbourConcentrationPool) * adjustedAmountToShare;
						neighbour.ReceiveConcentration(proportionalAmountToShare);
						concentration -= proportionalAmountToShare;
					}
				}
			}
		}

		private void TryCreateNewNeighbours() {
			var spreadsLeft = Mathf.FloorToInt(concentration / gasProps.SpreadMinConcentration) - 1;
			if (spreadsLeft <= 0) return;
			var viableCells = GetSpreadableAdacentCells();
			for (int i = 0; i < viableCells.Count; i++) {
				if (spreadsLeft <= 0) break;
				var targetPosition = viableCells[i];
				var newCloud = (GasCloud)ThingMaker.MakeThing(def);
				newCloud.BeginSpreadingTransition(this, targetPosition);
				GenPlace.TryPlaceThing(newCloud, targetPosition, ThingPlaceMode.Direct);
				spreadsLeft--;
			}
		}

	}
}
