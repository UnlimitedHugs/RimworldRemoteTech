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
				foreach (var link in node.GetAllNetworkLinks()) {
					Thing parent1 = link.First.parent, parent2 = link.Second.parent;
					var linkColor = link.CanTraverse ? SimpleColor.White : SimpleColor.Red;
					if (link.CanTraverse || Time.realtimeSinceStartup % 1f > .5f) {
						GenDraw.DrawLineBetween(parent1.TrueCenter(), parent2.TrueCenter(), linkColor);
					}
				}
			}
		}
	}
}