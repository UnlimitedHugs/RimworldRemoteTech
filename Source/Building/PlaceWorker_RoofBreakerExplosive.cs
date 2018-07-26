using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/**
	 * Draws the effective area of a shaped charge, changing color depending on its ability to break any thick mountain roof.
	 * Also highlights any thick mountain roof near the effective area.
	 */
	public class PlaceWorker_RoofBreakerExplosive : PlaceWorker {
		private const float AdditionalRoofDisplayRadius = 3f;

		private static readonly Color IneffectivePlacementColor = Color.yellow;
		private static readonly Color EffectivePlacementColor = Color.green;
		private static readonly List<IntVec3> effectiveRadiusCells = new List<IntVec3>();
		private static readonly List<IntVec3> overheadMountainCells = new List<IntVec3>();

		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol) {
			var effectiveRadius = RemoteExplosivesUtility.TryGetExplosiveRadius(def);
			if (effectiveRadius <= 0) return;
			var map = Find.CurrentMap;
			if(map == null) return;
			var roofGrid = map.roofGrid;
			effectiveRadiusCells.Clear();
			overheadMountainCells.Clear();
			var effectiveRadiusNumCells = GenRadial.NumCellsInRadius(effectiveRadius);
			var roofDisplayRadiusNumCells = GenRadial.NumCellsInRadius(effectiveRadius + AdditionalRoofDisplayRadius);
			// collect cells to display
			for (int i = 0; i < roofDisplayRadiusNumCells; i++) {
				var cell = center + GenRadial.RadialPattern[i];
				if (!cell.InBounds(map)) continue;
				var roof = roofGrid.RoofAt(cell);
				if (roof != null && roof.isThickRoof) {
					overheadMountainCells.Add(cell);
				}
				var cellInsideEffectiveRadius = i < effectiveRadiusNumCells;
				if (cellInsideEffectiveRadius) {
					effectiveRadiusCells.Add(cell);
				}
			}
			if (overheadMountainCells.Count > 0 && Find.Selector.NumSelected <= 1) { // don't draw overlay when multiple charges are selected
				GenDraw.DrawFieldEdges(overheadMountainCells, Color.white);
			}
			var effectiveRadiusColor = RemoteExplosivesUtility.IsEffectiveRoofBreakerPlacement(effectiveRadius, center, map) ? EffectivePlacementColor : IneffectivePlacementColor;
			GenDraw.DrawFieldEdges(effectiveRadiusCells, effectiveRadiusColor);
		}
	}
}