using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Draws the effective area of a shaped charge, changing color depending on its ability to break any thick mountain roof.
	/// Also: highlights any thick mountain roof near the effective area, as well as the effective areas of any other charges
	/// or blueprints of the same type.
	/// </summary>
	public class PlaceWorker_RoofBreakerExplosive : PlaceWorker {
		private const float AdditionalRoofDisplayRadius = 3f;

		private static readonly Color ThickRoofHighlightColor = new Color(1f, 1f, 1f, .5f);
		private static readonly Color IneffectivePlacementColor = new Color(1f, 0.9215686f, 0.01568628f, .5f);
		private static readonly Color EffectivePlacementColor = new Color(0f, 1f, 0f, .5f);
		private static readonly Color OtherEffectiveAreasColor = new Color(.3f, .7f, .3f, .15f);
		private static readonly List<IntVec3> cellBuffer = new List<IntVec3>();
		
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null) {
			var ownEffectiveRadius = RemoteTechUtility.TryGetExplosiveRadius(def);
			var map = Find.CurrentMap;
			if (map == null || ownEffectiveRadius <= 0) return;
			if (Find.Selector.NumSelected <= 1) {
				// highlight nearby thick mountain roof cells
				var fogGrid = map.fogGrid;
				var roofGrid = map.roofGrid;
				GatherCellsInRadius(center, map, ownEffectiveRadius + AdditionalRoofDisplayRadius,
					cell => fogGrid.IsFogged(cell) || (roofGrid.RoofAt(cell)?.isThickRoof ?? false)
				);
				OverlayDrawer.DrawFieldEdges(cellBuffer, ThickRoofHighlightColor);

				void DrawMatchingEdgesAroundThing(Thing t) {
					GatherCellsInRadius(t.Position, map, ownEffectiveRadius);
					OverlayDrawer.DrawSolidField(cellBuffer, OtherEffectiveAreasColor);
				}

				// highlight effective areas of already built charges of same type
				var colonistBuildings = map.listerBuildings.allBuildingsColonist;
				for (var i = 0; i < colonistBuildings.Count; i++) {
					if (colonistBuildings[i]?.def == def) {
						DrawMatchingEdgesAroundThing(colonistBuildings[i]);
					}
				}
				// highlight effective areas of blueprints for charges of same type
				var blueprints = map.listerThings.ThingsMatching(ThingRequest.ForGroup(ThingRequestGroup.Blueprint));
				for (var i = 0; i < blueprints.Count; i++) {
					if (blueprints[i]?.def?.entityDefToBuild == def) {
						DrawMatchingEdgesAroundThing(blueprints[i]);	
					}
				}
			}
			// highlight own effective radius with color-coded effectiveness
			var effectiveRadiusColor = RemoteTechUtility.IsEffectiveRoofBreakerPlacement(ownEffectiveRadius, center, map, true)
				? EffectivePlacementColor
				: IneffectivePlacementColor;
			GatherCellsInRadius(center, map, ownEffectiveRadius);
			OverlayDrawer.DrawFieldEdges(cellBuffer, effectiveRadiusColor);
		}

		private static void GatherCellsInRadius(IntVec3 centerCell, Map map, float radius, Predicate<IntVec3> cellFilter = null) {
			cellBuffer.Clear();
			var numCellsInRadius = GenRadial.NumCellsInRadius(radius);
			for (int i = 0; i < numCellsInRadius; i++) {
				var cell = centerCell + GenRadial.RadialPattern[i];
				if (cell.InBounds(map) && (cellFilter == null || cellFilter(cell))) {
					cellBuffer.Add(cell);
				}
			}
		}

		private static class OverlayDrawer {
			private static readonly bool[] rotNeeded = new bool[4];
			private static BoolGrid fieldGrid;

			public static void DrawSolidField(List<IntVec3> cells, Color color) {
				var material = MaterialPool.MatFrom(new MaterialRequest {
					shader = ShaderDatabase.MetaOverlay,
					color = color, 
					mainTex = BaseContent.WhiteTex
				});
				for (var i = 0; i < cells.Count; i++) {
					Graphics.DrawMesh(MeshPool.plane10, cells[i].ToVector3Shifted(), Quaternion.identity, material, 0);
				}
			}

			public static void DrawFieldEdges(List<IntVec3> cells, Color color) {
				// swiped from GenDraw.DrawFieldEdges- we need to draw using a different shader to avoid being covered by fog
				var currentMap = Find.CurrentMap;
				var material = MaterialPool.MatFrom(new MaterialRequest {
					shader = ShaderDatabase.MetaOverlay,
					color = color,
					BaseTexPath = "UI/Overlays/TargetHighlight_Side"
				});
				material.mainTexture.wrapMode = TextureWrapMode.Clamp;
				if (fieldGrid == null) {
					fieldGrid = new BoolGrid(currentMap);
				} else {
					fieldGrid.ClearAndResizeTo(currentMap);
				}
				int x = currentMap.Size.x;
				int z = currentMap.Size.z;
				int count = cells.Count;
				for (int i = 0; i < count; i++) {
					if (cells[i].InBounds(currentMap)) {
						fieldGrid[cells[i].x, cells[i].z] = true;
					}
				}
				for (int j = 0; j < count; j++) {
					var intVec = cells[j];
					if (intVec.InBounds(currentMap)) {
						rotNeeded[0] = (intVec.z < z - 1 && !fieldGrid[intVec.x, intVec.z + 1]);
						rotNeeded[1] = (intVec.x < x - 1 && !fieldGrid[intVec.x + 1, intVec.z]);
						rotNeeded[2] = (intVec.z > 0 && !fieldGrid[intVec.x, intVec.z - 1]);
						rotNeeded[3] = (intVec.x > 0 && !fieldGrid[intVec.x - 1, intVec.z]);
						for (int k = 0; k < 4; k++) {
							if (rotNeeded[k]) {
								Graphics.DrawMesh(MeshPool.plane10, intVec.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays), 
									new Rot4(k).AsQuat, material, 0);
							}
						}
					}
				}
			}
		}
	}
}