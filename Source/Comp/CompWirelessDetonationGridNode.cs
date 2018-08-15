using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Represents a node that can connect to other node-buildings in signal range and recursively form a network.
	/// </summary>
	public class CompWirelessDetonationGridNode : ThingComp {
		public struct TransmitterReceiverPair {
			public readonly CompWirelessDetonationGridNode Transmitter;
			public readonly IWirelessDetonationReceiver Receiver;
			public TransmitterReceiverPair(CompWirelessDetonationGridNode transmitter, IWirelessDetonationReceiver receiver) {
				Transmitter = transmitter;
				Receiver = receiver;
			}
		}
		// order-invariant node pair
		public struct NetworkGraphLink : IEquatable<NetworkGraphLink> {
			public readonly CompWirelessDetonationGridNode First;
			public readonly CompWirelessDetonationGridNode Second;
			public readonly bool CanTraverse;
			public NetworkGraphLink(CompWirelessDetonationGridNode first, CompWirelessDetonationGridNode second, bool canTraverse) {
				First = first;
				Second = second;
				CanTraverse = canTraverse;
			}
			public override bool Equals(object obj) {
				if (!(obj is NetworkGraphLink pair)) return false;
				return Equals(pair);
			}
			public bool Equals(NetworkGraphLink other) {
				return First == other.First && Second == other.Second || First == other.Second && Second == other.First;
			}
			public override int GetHashCode() {
				var one = First != null ? First.GetHashCode() : 0;
				var two = Second != null ? Second.GetHashCode() : 0;
				return Gen.HashCombineInt(one < two ? one : two, one < two ? two : one); 
			}
		}

		private const int UpdateAdjacentNodesEveryTicks = 30;

		// allows to do a global reset of all adjacency caches
		private static int globalRecacheId;

		public static IEnumerable<CompWirelessDetonationGridNode> GetPotentialNeighborsFor(ThingDef def, IntVec3 pos, Map map) {
			var radius = def.GetStatValueAbstract(Resources.Stat.rxSignalRange);
			if (radius > 0f) {
				var endpoint = (def.GetCompProperties<CompProperties_WirelessDetonationGridNode>()?.endpoint).GetValueOrDefault();
				var candidates = map.listerBuildings.allBuildingsColonist;
				for (var i = 0; i < candidates.Count; i++) {
					CompWirelessDetonationGridNode comp;
					if (candidates[i] is ThingWithComps building
						&& (comp = building.GetComp<CompWirelessDetonationGridNode>()) != null
						&& building.Position.DistanceTo(pos) <= Mathf.Min(radius, comp.Radius)
						&& (endpoint == false || !comp.Props.endpoint)) {
						yield return comp;
					}
				}
			}
		}

		public CompProperties_WirelessDetonationGridNode Props {
			get { return props as CompProperties_WirelessDetonationGridNode; }
		}

		public bool CanTransmit {
			get { return powerComp == null || powerComp.PowerOn; }
		}

		public float Radius {
			get { return parent.GetStatValue(Resources.Stat.rxSignalRange); }
		}

		private CompPowerTrader powerComp;
		private int lastRecacheTick;
		private int lastGlobalRecacheId;
		private List<CompWirelessDetonationGridNode> adjacentNodes;

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			base.PostSpawnSetup(respawningAfterLoad);
			powerComp = parent.GetComp<CompPowerTrader>();
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

		public override void PostDeSpawn(Map map) {
			base.PostDeSpawn(map);
			globalRecacheId = Rand.Int;
		}

		// enumerates pairs of receivers and the node closest to them
		public IEnumerable<TransmitterReceiverPair> FindReceiversInNetworkRange() {
			var receivers = new HashSet<IWirelessDetonationReceiver>();
			var transmitters = new HashSet<CompWirelessDetonationGridNode>();
			foreach (var transmitter in GetReachableNetworkNodes()) {
				foreach (var receiver in transmitter.FindReceiversInNodeRange()) {
					transmitters.Add(transmitter);
					receivers.Add(receiver);
				}
			}
			// for each receiver pick the closest transmitter
			return receivers.Select(r => {
				var closest = transmitters.Select(t => 
						new KeyValuePair<CompWirelessDetonationGridNode, float>(t, t.parent.Position.DistanceToSquared(r.Position))
				).Aggregate((min, pair) => min.Value == 0 || pair.Value < min.Value ? pair : min);
				return new TransmitterReceiverPair(closest.Key, r);
			});

		}

		// finds buildings as well as their comps
		public IEnumerable<IWirelessDetonationReceiver> FindReceiversInNodeRange() {
			if (!CanTransmit) yield break;
			float radius = Radius;
			var sample = parent.Map.listerBuildings.allBuildingsColonist;
			foreach (var building in sample) {
				if (building.Position.DistanceTo(parent.Position) > radius) continue;
				if (building is IWirelessDetonationReceiver br) {
					yield return br;
				} else {
					for (int i = 0; i < building.AllComps.Count; i++) {
						// ReSharper disable once SuspiciousTypeConversion.Global
						if (building.AllComps[i] is IWirelessDetonationReceiver comp)
							yield return comp;
					}
				}
			}
		}

		public IEnumerable<NetworkGraphLink> GetAllNetworkLinks() {
			var links = new HashSet<NetworkGraphLink>();
			TraverseNetwork(false, null, l => links.Add(l));
			return links;
		}

		public IEnumerable<CompWirelessDetonationGridNode> GetReachableNetworkNodes() {
			var nodes = new HashSet<CompWirelessDetonationGridNode>();
			TraverseNetwork(true, n => nodes.Add(n));
			return nodes;
		}

		public List<CompWirelessDetonationGridNode> GetAdjacentNodes() {
			RecacheAdjacentNodesIfNeeded();
			return adjacentNodes;
		}

		public void TraverseNetwork(bool reachableOnly, Action<CompWirelessDetonationGridNode> nodeCallback, Action<NetworkGraphLink> linkCallback = null) {
			var nodes = new HashSet<CompWirelessDetonationGridNode>();
			var queue = new Queue<CompWirelessDetonationGridNode>();
			queue.Enqueue(this);
			while (queue.Count > 0) {
				var comp = queue.Dequeue();
				// don't walk through endpoints, unless we're starting from one
				if (nodes.Add(comp) && (comp == this || !comp.Props.endpoint)) {
					nodeCallback?.Invoke(comp);
					var compAdjacent = comp.GetAdjacentNodes();
					for (var i = 0; i < compAdjacent.Count; i++) {
						var adjacent = compAdjacent[i];
						var canTraverse = comp.CanTransmit && adjacent.CanTransmit;
						if (canTraverse || !reachableOnly) {
							linkCallback?.Invoke(new NetworkGraphLink(comp, adjacent, canTraverse));
							queue.Enqueue(adjacent);
						}
					}
				}
			}
		}

		private void RecacheAdjacentNodesIfNeeded() {
			if (lastRecacheTick + UpdateAdjacentNodesEveryTicks <= Find.TickManager.TicksGame || globalRecacheId != lastGlobalRecacheId) {
				lastGlobalRecacheId = globalRecacheId;
				var map = parent.Map;
				var center = parent.Position;
				var radius = Radius;
				adjacentNodes = adjacentNodes ?? new List<CompWirelessDetonationGridNode>();
				adjacentNodes.Clear();
				lastRecacheTick = Find.TickManager.TicksGame;
				var candidates = map.listerBuildings.allBuildingsColonist;
				for (var i = 0; i < candidates.Count; i++) {
					CompWirelessDetonationGridNode comp;
					if (candidates[i] is ThingWithComps building 
						&& building != parent 
						&& (comp = building.GetComp<CompWirelessDetonationGridNode>()) != null) {
						var mutualMaxRange = Mathf.Min(radius, comp.Radius);
						if (building.Position.DistanceTo(center) <= mutualMaxRange
							&& (Props.endpoint == false || Props.endpoint != comp.Props.endpoint)) {
							adjacentNodes.Add(comp);
						}
					}
				}
			}
		}
	}
}