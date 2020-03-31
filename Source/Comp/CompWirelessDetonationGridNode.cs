using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Represents a node that can connect to other node-buildings in signal range and recursively form a network.
	/// Requires the rxSignalRange stat to be set on the parent thing.
	/// Also uses IWirelessDetonationReceiver when looking for receivers in range.
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

		// saved
		private bool _enabled = true;
		public bool Enabled {
			get { return _enabled; }
			set {
				if (_enabled != value) {
					_enabled = value;
					RecacheAllNodes();
				}
			}
		}

		public CompProperties_WirelessDetonationGridNode Props {
			get { return props as CompProperties_WirelessDetonationGridNode; }
		}

		public bool CanTransmit {
			get { return Enabled && powerComp == null || powerComp.PowerOn; }
		}

		public float Radius {
			get { return parent.GetStatValue(Resources.Stat.rxSignalRange); }
		}

		public IntVec3 Position {
			get { return RemoteTechUtility.GetHighestHolderInMap(parent).Position; }
		}

		private CompPowerTrader powerComp;
		private int lastRecacheTick;
		private int lastGlobalRecacheId;
		private List<CompWirelessDetonationGridNode> adjacentNodes;

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			base.PostSpawnSetup(respawningAfterLoad);
			powerComp = parent.GetComp<CompPowerTrader>();
			if (Radius < float.Epsilon) {
				RemoteTechController.Instance.Logger.Error($"CompWirelessDetonationGridNode has zero radius. Missing signal range property on def {parent.def.defName}?");
			}
			if (Props == null) {
				RemoteTechController.Instance.Logger.Error($"CompWirelessDetonationGridNode needs CompProperties_WirelessDetonationGridNode on def {parent.def.defName}");
			}
			if (!respawningAfterLoad) {
				RecacheAllNodes();
			}
		}

		public override void PostDeSpawn(Map map) {
			base.PostDeSpawn(map);
			globalRecacheId = Rand.Int;
		}

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Values.Look(ref _enabled, "wirelessNodeEnabled", true);
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
			var ownPos = Position;
			return receivers.Select(r => {
				var closest = transmitters.Select(t => 
					new KeyValuePair<CompWirelessDetonationGridNode, float>(t, ownPos.DistanceToSquared(r.Position))
				).Aggregate((min, pair) => min.Value < float.Epsilon || pair.Value < min.Value ? pair : min);
				return new TransmitterReceiverPair(closest.Key, r);
			});

		}

		// finds buildings as well as their comps
		public IEnumerable<IWirelessDetonationReceiver> FindReceiversInNodeRange() {
			if (!CanTransmit) yield break;
			float radius = Radius;
			var map = ThingOwnerUtility.GetRootMap(parent.ParentHolder);
			var sample = map.listerBuildings.allBuildingsColonist;
			var ownPos = Position;
			foreach (var building in sample) {
				if (building.Position.DistanceTo(ownPos) > radius) continue;
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

		public void DrawNetworkLinks() {
			foreach (var link in GetAllNetworkLinks()) {
				IntVec3 pos1 = link.First.Position, pos2 = link.Second.Position;
				var linkColor = link.CanTraverse ? SimpleColor.White : SimpleColor.Red;
				if (link.CanTraverse || Time.realtimeSinceStartup % 1f > .5f) {
					GenDraw.DrawLineBetween(pos1.ToVector3Shifted(), pos2.ToVector3Shifted(), linkColor);
				}
			}
		}

		public void DrawRadiusRing(bool drawReceivers = false) {
			var radius = Radius;
			if (radius <= GenRadial.MaxRadialPatternRadius) {
				var ownPos = Position;
				GenDraw.DrawRadiusRing(ownPos, radius);
				if (drawReceivers) {
					foreach (var receiver in FindReceiversInNodeRange()) {
						// highlight explosives in range
						var drawPos = receiver.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
						Graphics.DrawMesh(MeshPool.plane10, drawPos, Quaternion.identity, GenDraw.InteractionCellMaterial, 0);
					}
				}
			}
		}

		private void RecacheAdjacentNodesIfNeeded() {
			if (lastRecacheTick + UpdateAdjacentNodesEveryTicks <= Find.TickManager.TicksGame || globalRecacheId != lastGlobalRecacheId) {
				lastGlobalRecacheId = globalRecacheId;
				var map = ThingOwnerUtility.GetRootMap(parent.ParentHolder);
				var center = Position;
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

		private static void RecacheAllNodes() {
			globalRecacheId = Rand.Int;
		}
	}
}