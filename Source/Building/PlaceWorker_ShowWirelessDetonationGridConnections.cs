using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	public class PlaceWorker_ShowWirelessDetonationGridConnections : PlaceWorker, ISelectedThingPlaceWorker {
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol) {
			var radius = def.GetStatValueAbstract(Resources.Stat.rxSignalRange);
			if (radius > 0f) {
				var map = Find.CurrentMap;
				var endpoint = (def.GetCompProperties<CompProperties_WirelessDetonationGridNode>()?.endpoint).GetValueOrDefault();
				var candidates = map.listerBuildings.allBuildingsColonist;
				for (var i = 0; i < candidates.Count; i++) {
					CompWirelessDetonationGridNode comp;
					if (candidates[i] is ThingWithComps building
						&& (comp = building.GetComp<CompWirelessDetonationGridNode>()) != null
						&& building.Position.DistanceTo(center) <= radius
						&& (endpoint == false || !comp.Props.endpoint)) {
						GenDraw.DrawLineBetween(center.ToVector3(), building.Position.ToVector3());
					}
				}
			}
		}

		public void DrawGhostForSelected(Thing thing) {
			CompWirelessDetonationGridNode baseNode;
			if (thing is ThingWithComps twc && (baseNode = twc.GetComp<CompWirelessDetonationGridNode>()) != null) {
				foreach (var node in baseNode.GetAllConnectedNodes()) {
					if (!node.Props.endpoint) {
						foreach (var adjacentNode in node.GetAdjacentNodes()) {
							Thing parent1 = node.parent, parent2 = adjacentNode.parent;
							// make sure we draw our lines only once, not twice
							if (parent1.thingIDNumber < parent2.thingIDNumber || node.Props.endpoint != adjacentNode.Props.endpoint) {
								GenDraw.DrawLineBetween(parent1.TrueCenter(), parent2.TrueCenter());
							}
						}
					}
				}
			}
		}
	}
}