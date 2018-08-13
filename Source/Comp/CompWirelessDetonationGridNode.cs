using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Represents a node that can connect to other node-buildings in signal range and recursively form a network.
	/// </summary>
	public class CompWirelessDetonationGridNode : ThingComp {
		private const int UpdateAdjacentNodesEveryTicks = 60;

		// allows to do a global reset of all cached adjacency
		private static int globalRecacheId;

		public CompProperties_WirelessDetonationGridNode Props {
			get { return props as CompProperties_WirelessDetonationGridNode; }
		}

		private float Radius {
			get { return parent.GetStatValue(Resources.Stat.rxSignalRange); }
		}

		private int lastRecacheTick;
		private int lastGlobalRecacheId;
		private List<CompWirelessDetonationGridNode> adjacentNodes;

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			base.PostSpawnSetup(respawningAfterLoad);
			if (Radius == 0) {
				RemoteExplosivesController.Instance.Logger.Error($"CompWirelessDetonationGridNode has zero radius. Missing signal range property on def {parent.def.defName}?");
			}
			if (Props == null) {
				RemoteExplosivesController.Instance.Logger.Error($"CompWirelessDetonationGridNode needs CompProperties_WirelessDetonationGridNode on def {parent.def.defName}");
			}
			if (!respawningAfterLoad) {
				globalRecacheId = Rand.Int;
			}
		}

		public IEnumerable<CompWirelessDetonationGridNode> GetAllConnectedNodes() {
			var nodes = new HashSet<CompWirelessDetonationGridNode>();
			var queue = new Queue<CompWirelessDetonationGridNode>();
			queue.Enqueue(this);
			while (queue.Count > 0) {
				var comp = queue.Dequeue();
				// don't walk through endpoints, unless we're starting from one
				if (nodes.Add(comp) && (comp == this || !comp.Props.endpoint)) {
					var compAdjacent = comp.GetAdjacentNodes();
					for (var i = 0; i < compAdjacent.Count; i++) {
						queue.Enqueue(compAdjacent[i]);
					}
				}
			}
			return nodes;
		}

		public List<CompWirelessDetonationGridNode> GetAdjacentNodes() {
			RecacheAdjacentNodesIfNeeded();
			return adjacentNodes;
		}

		private void RecacheAdjacentNodesIfNeeded() {
			if (lastRecacheTick + UpdateAdjacentNodesEveryTicks <= Find.TickManager.TicksGame || globalRecacheId != lastGlobalRecacheId) {
				lastGlobalRecacheId = globalRecacheId;
				var map = parent.Map;
				var center = parent.Position;
				var radius = Radius;
				var candidates = map.listerBuildings.allBuildingsColonist;
				adjacentNodes = adjacentNodes ?? new List<CompWirelessDetonationGridNode>();
				adjacentNodes.Clear();
				lastRecacheTick = Find.TickManager.TicksGame;
				for (var i = 0; i < candidates.Count; i++) {
					CompWirelessDetonationGridNode comp;
					if (candidates[i] is ThingWithComps building
						&& building != parent
						&& (comp = building.GetComp<CompWirelessDetonationGridNode>()) != null
						&& building.Position.DistanceTo(center) <= radius
						&& (Props.endpoint == false || Props.endpoint != comp.Props.endpoint)) {
						adjacentNodes.Add(comp);
					}
				}
			}
		}
	}
}