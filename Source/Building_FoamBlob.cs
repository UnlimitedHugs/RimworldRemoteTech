using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	public class Building_FoamBlob : Building {
		private static readonly SoundDef spraySound = SoundDef.Named("RemoteFoamSpray");
		private static readonly SoundDef solidifySound = SoundDef.Named("RemoteFoamSolidify");
		
		private const float animationDuration = 1f;
		private const float animationMagnitude = 1.5f;

		private BuildingProperties_FoamBlob foamProps;
		
		private int ticksUntilHardened = -1;
		private int numSpreadsLeft;
		private int ticksUntilNextSpread;
		[Unsaved]
		private readonly DisposablePrimitiveWrapper<float> animationProgress = new DisposablePrimitiveWrapper<float>(1);
		[Unsaved]
		public Vector2 spriteScaleMultiplier = new Vector2(1f, 1f);

		private List<IntVec3> adjacentCells;
		private bool justCreated;

		public override void SpawnSetup() {
			base.SpawnSetup();
			foamProps = (BuildingProperties_FoamBlob)def.building;
			if(justCreated) {
				SetFactionDirect(Faction.OfColony);
				ticksUntilHardened = foamProps.ticksToHarden.RandomInRange;
				spraySound.PlayOneShot(this);
				PrimeSpawnAnimation();
				justCreated = false;
			}
		}

		public override void PostMake() {
			base.PostMake();
			justCreated = true;
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref ticksUntilHardened, "ticksUntilHardened", 0);
			Scribe_Values.LookValue(ref numSpreadsLeft, "numSpreadsLeft", 0);
			Scribe_Values.LookValue(ref ticksUntilNextSpread, "ticksUntilNextSpread", 0);
		}

		public void SetSpreadingCharges(int numCharges) {
			numSpreadsLeft = numCharges;
			ticksUntilNextSpread = foamProps.ticksBetweenSpreading.RandomInRange;
		}

		private void PrimeSpawnAnimation() {
			animationProgress.Value = 0f;
			ValueInterpolator.Instance.InterpolateValue(animationProgress, 1f, animationDuration, ValueInterpolator.InterpolationCurveType.QuinticEaseOut);
		}

		private void Harden() {
			Destroy();
			var wallTile = ThingMaker.MakeThing(foamProps.hardenedDef);
			wallTile.SetFactionDirect(Faction.OfColony);
			GenPlace.TryPlaceThing(wallTile, Position, ThingPlaceMode.Direct);
			solidifySound.PlayOneShot(this);
		}

		public override void Tick() {
			base.Tick();
			if (numSpreadsLeft > 0) {
				ticksUntilNextSpread--;
				if(ticksUntilNextSpread<=0) {
					SpreadFoam();
					numSpreadsLeft--;
					ticksUntilNextSpread = foamProps.ticksBetweenSpreading.RandomInRange;
				}
			}
			ticksUntilHardened--;
			if(ticksUntilHardened<=0) {
				Harden();
			}
		}

		public override void Draw() {
			if (animationProgress < 1) {
				//animationProgress += 1 / (float)numFramesInAnimation;
				//if (animationProgress > 1) animationProgress = 1;
				
				//var easedProgress = RemoteExplosivesUtility.QuinticEaseOut(animationProgress, 0, 1f, 1f);
				var easedProgress = animationProgress;
				const float delta = .5f * animationMagnitude;
				var fixedScalar = easedProgress;
				var xScalar = 1f + delta - delta * easedProgress;
				var yScalar = (1f - delta) + delta * easedProgress;
				spriteScaleMultiplier = new Vector2(fixedScalar * xScalar, fixedScalar * yScalar);
			}
			base.Draw();
		}

		public override string GetInspectString() {
			return string.Format("FoamBlob_solidify_progress".Translate(), 100-Mathf.Ceil((ticksUntilHardened / (float)foamProps.ticksToHarden.max) * 100));
		}

		private void SpreadFoam() {
			const int maxSearchDistance = 10;
			var newFoam = ThingMaker.MakeThing(def);
			var targetCell = TryFindNearestCellForNewBlob(Position, maxSearchDistance);
			if(targetCell == Position) return;
			GenPlace.TryPlaceThing(newFoam, targetCell, ThingPlaceMode.Direct);
		}

		// find a stadable cell that can be reached from the given position. Foam blobs count as traversable.
		// vanilla closewalk stuff is no good, since foam must spread on an uniterrupted path
		private IntVec3 TryFindNearestCellForNewBlob(IntVec3 originalPosition, int maxDistance) {
			var cellQueue = new Queue<IntVec3>();
			var visistedCells = new HashSet<IntVec3>();
			cellQueue.Enqueue(originalPosition);
			while (cellQueue.Count > 0) {
				var cell = cellQueue.Dequeue();
				var standable = cell.Standable();
				var containsFoam = Find.ThingGrid.CellContains(cell, def);
				if (standable) {
					return cell;
				} else if (containsFoam) {
					foreach (var adjacentCell in GetRandomizedAdjacentCellsCardinalFirst(cell)) {
						if (Mathf.Sqrt(adjacentCell.DistanceToSquared(originalPosition)) > maxDistance || visistedCells.Contains(adjacentCell)) continue;
						cellQueue.Enqueue(adjacentCell);
					}
				}
				visistedCells.Add(cell);
			}
			return originalPosition;
		}

		private IEnumerable<IntVec3> GetRandomizedAdjacentCellsCardinalFirst(IntVec3 pos) {
			if(adjacentCells==null) {
				adjacentCells = new List<IntVec3>(8);
			} else {
				adjacentCells.Clear();
			}
			foreach (var cardinalDir in GenAdj.CardinalDirections) {
				adjacentCells.Add(pos + cardinalDir);
			}
			adjacentCells.Shuffle();
			foreach (var diagonalDir in GenAdj.DiagonalDirectionsAround) {
				adjacentCells.Add(pos + diagonalDir);
			}
			return adjacentCells;
		} 
	}
}
