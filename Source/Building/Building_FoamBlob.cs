using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteTech {
	/* 
	 * Created by the sealing foam canister. 
	 * Will spread a given number of times and turn into another building (wall) once time runs out.
	 */
	public class Building_FoamBlob : Building {
		private const float animationDuration = 1f;
		private const float animationMagnitude = 1.5f;

		private BuildingProperties_FoamBlob foamProps;
		
		private int ticksUntilHardened = -1;
		private int numSpreadsLeft;
		private int ticksUntilNextSpread;
		private readonly ValueInterpolator animationProgress = new ValueInterpolator(1f);
		public Vector2 spriteScaleMultiplier = new Vector2(1f, 1f);

		private List<IntVec3> adjacentCells;
		private bool justCreated;

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			foamProps = (BuildingProperties_FoamBlob)def.building;
			this.RequireComponent(foamProps);
			if(justCreated) {
				SetFactionDirect(Faction.OfPlayer);
				ticksUntilHardened = foamProps.ticksToHarden.RandomInRange;
				Resources.Sound.rxFoamSpray.PlayOneShot(this);
				PrimeSpawnAnimation();
				justCreated = false;
				ticksUntilNextSpread = foamProps.ticksBetweenSpreading.RandomInRange;
			}
		}

		public override void PostMake() {
			base.PostMake();
			justCreated = true;
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref ticksUntilHardened, "ticksUntilHardened");
			Scribe_Values.Look(ref numSpreadsLeft, "numSpreadsLeft");
			Scribe_Values.Look(ref ticksUntilNextSpread, "ticksUntilNextSpread");
		}

		public void AddSpreadingCharges(int numCharges) {
			numSpreadsLeft += numCharges;
		}

		private void PrimeSpawnAnimation() {
			animationProgress.value = 0;
			animationProgress.StartInterpolation(1f, animationDuration, CurveType.QuinticOut);
		}

		private void Harden() {
			var pos = Position;
			var map = Map;
			Destroy();
			var wallTile = ThingMaker.MakeThing(foamProps.hardenedDef);
			wallTile.SetFactionDirect(Faction.OfPlayer);
			GenPlace.TryPlaceThing(wallTile, pos, map, ThingPlaceMode.Direct);
			Resources.Sound.rxFoamSolidify.PlayOneShot(this);
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
			UpdateScale();
			base.Draw();
		}

		public override string GetInspectString() {
			return string.Format("FoamBlob_solidify_progress".Translate(), 100-Mathf.Ceil((ticksUntilHardened / (float)foamProps.ticksToHarden.max) * 100));
		}

		// scale the sprite non-uniformly for a more interesting visual effect
		private void UpdateScale() {
			animationProgress.UpdateIfUnpaused();
			var easedProgress = animationProgress.value;
			const float delta = .5f * animationMagnitude;
			var fixedScalar = easedProgress;
			var xScalar = 1f + delta - delta * easedProgress;
			var yScalar = (1f - delta) + delta * easedProgress;
			spriteScaleMultiplier = new Vector2(fixedScalar * xScalar, fixedScalar * yScalar);
		}

		private void SpreadFoam() {
			const int maxSearchDistance = 10;
			var targetCell = TryFindNearestCellForNewBlob(Position, Map, maxSearchDistance);
			if(targetCell == Position) return;
			var newFoam = ThingMaker.MakeThing(def);
			GenPlace.TryPlaceThing(newFoam, targetCell, Map, ThingPlaceMode.Direct);
		}

		// find a standable cell that can be reached from the given position. Foam blobs count as traversible.
		// vanilla closewalk stuff is no good, since foam must spread on an uninterrupted path
		private IntVec3 TryFindNearestCellForNewBlob(IntVec3 originalPosition, Map map, int maxDistance) {
			var cellQueue = new Queue<IntVec3>();
			var visitedCells = new HashSet<IntVec3>();
			cellQueue.Enqueue(originalPosition);
			while (cellQueue.Count > 0) {
				var cell = cellQueue.Dequeue();
				var standable = cell.Standable(map);
				var containsFoam = map.thingGrid.CellContains(cell, def);
				if (standable) {
					return cell;
				} else if (containsFoam) {
					foreach (var adjacentCell in GetRandomizedAdjacentCellsCardinalFirst(cell)) {
						if (Mathf.Sqrt(adjacentCell.DistanceToSquared(originalPosition)) > maxDistance || visitedCells.Contains(adjacentCell)) continue;
						cellQueue.Enqueue(adjacentCell);
					}
				}
				visitedCells.Add(cell);
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
