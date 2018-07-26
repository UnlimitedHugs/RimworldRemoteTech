using UnityEngine;
using Verse;

namespace RemoteExplosives {
	public class PlaceWorker_GasVent : PlaceWorker {
		private readonly Color DefaultArrowColor = Color.white;
		private readonly Color BlockedArrowColor = Color.red;

		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol) {
			var map = Find.CurrentMap;
			if (map == null) return;
			var targetCell = center + IntVec3Utility.RotatedBy(IntVec3.North, rot);
			var sourceCell = center + IntVec3Utility.RotatedBy(IntVec3.South, rot);
			if (!targetCell.InBounds(map) || !sourceCell.InBounds(map)) {
				return;
			}
			DrawArrow(sourceCell, rot, sourceCell.Impassable(map) ? BlockedArrowColor : DefaultArrowColor);
			DrawArrow(targetCell, rot, targetCell.Impassable(map) ? BlockedArrowColor : DefaultArrowColor);
		}

		private void DrawArrow(IntVec3 pos, Rot4 rot, Color color) {
			var material = MaterialPool.MatFrom(Resources.Textures.gas_vent_arrow, ShaderDatabase.TransparentPostLight, color);
			Graphics.DrawMesh(MeshPool.plane10, pos.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays), rot.AsQuat, material, 0);
		}
	}
}