using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Draws a radius ring based on the signal range stat
	/// </summary>
	public class PlaceWorker_DetonatorRadius : PlaceWorker, ISelectedThingPlaceWorker {
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol) {
			var radiusStat = def.GetStatValueAbstract(Resources.Stat.rxSignalRange);
			if (radiusStat > 0f) {
				GenDraw.DrawRadiusRing(center, radiusStat);
			}
		}

		public void DrawGhostForSelected(Thing thing) {
			var node = (thing as ThingWithComps)?.GetComp<CompWirelessDetonationGridNode>();
			if (node != null) {
				GenDraw.DrawRadiusRing(thing.Position, node.Radius);
				foreach (var receiver in node.FindReceiversInNodeRange()) {
					// highlight explosives in range
					var drawPos = receiver.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
					Graphics.DrawMesh(MeshPool.plane10, drawPos, Quaternion.identity, GenDraw.InteractionCellMaterial, 0);
				}
			}
		}
	}
}