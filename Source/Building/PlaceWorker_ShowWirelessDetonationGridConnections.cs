using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	public class PlaceWorker_ShowWirelessDetonationGridConnections : PlaceWorker, ISelectedThingPlaceWorker {
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol) {
			var nodes = CompWirelessDetonationGridNode.GetPotentialNeighborsFor(def, center, Find.CurrentMap);
			foreach (var node in nodes) {
				GenDraw.DrawLineBetween(center.ToVector3(), node.parent.Position.ToVector3());
			}
		}

		public void DrawGhostForSelected(Thing thing) {
			CompWirelessDetonationGridNode node;
			if (thing is ThingWithComps twc && (node = twc.GetComp<CompWirelessDetonationGridNode>()) != null) {
				node.DrawNetworkLinks();
			}
		}
	}
}