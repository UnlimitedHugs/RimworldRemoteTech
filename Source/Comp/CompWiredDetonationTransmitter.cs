using System;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	// Transmits the wired detonation signal to signal receiver comps and transmitter comps on adjacent tiles. 
	public class CompWiredDetonationTransmitter : CompDetonationGridNode {
		public delegate bool AllowSignalPassage();

		public AllowSignalPassage signalPassageTest;
		private int lastSignalId;

		private CompProperties_WiredDetonationTransmitter CustomProps {
			get {
				if (!(props is CompProperties_WiredDetonationTransmitter)) throw new Exception("CompWiredDetonationTransmitter requires CompProperties_WiredDetonationTransmitter");
				return (CompProperties_WiredDetonationTransmitter) props;
			}
		}

		public override void PrintForDetonationGrid(SectionLayer layer) {
			PrintConnection(layer);
		}

		public void ReceiveSignal(int signalId, int signalSteps) {
			if (signalId == lastSignalId) return;
			if (signalPassageTest != null && !signalPassageTest()) return;
			lastSignalId = signalId;
			PassSignalToReceivers(signalSteps);
			ConductSignalToNeighbors(signalId, signalSteps);
		}

		private void PassSignalToReceivers(int signalSteps) {
			if (parent.Map == null) throw new Exception("null map");
			var delayOnThisTile = Mathf.RoundToInt(signalSteps * CustomProps.signalDelayPerTile);
			var thingsOnTile = parent.Map.thingGrid.ThingsListAtFast(parent.Position);
			for (var i = 0; i < thingsOnTile.Count; i++) {
				var comp = thingsOnTile[i].TryGetComp<CompWiredDetonationReceiver>();
				if (comp == null) continue;
				comp.ReceiveSignal(delayOnThisTile);
			}
		}

		private void ConductSignalToNeighbors(int signalId, int signalSteps) {
			if (parent.Map == null) throw new Exception("null map");
			var neighbors = GenAdj.CardinalDirectionsAround;
			for (var i = 0; i < neighbors.Length; i++) {
				var tileThings = parent.Map.thingGrid.ThingsListAtFast(neighbors[i] + parent.Position);
				for (int j = 0; j < tileThings.Count; j++) {
					var comp = tileThings[j].TryGetComp<CompWiredDetonationTransmitter>();
					if (comp == null) continue;
					comp.ReceiveSignal(signalId, signalSteps+1);
				}
			}
		}
	}
}