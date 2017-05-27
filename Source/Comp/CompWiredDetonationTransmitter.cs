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

		public void RecieveSignal(int signalId, int sinalSteps) {
			if (signalId == lastSignalId) return;
			if (signalPassageTest != null && !signalPassageTest()) return;
			lastSignalId = signalId;
			PassSignalToReceivers(sinalSteps);
			ConductSignalToNeighbours(signalId, sinalSteps);
		}

		private void PassSignalToReceivers(int sinalSteps) {
			if (parent.Map == null) throw new Exception("null map");
			var delayOnThisTile = Mathf.RoundToInt(sinalSteps * CustomProps.signalDelayPerTile);
			var thingsOnTile = parent.Map.thingGrid.ThingsListAtFast(parent.Position);
			for (var i = 0; i < thingsOnTile.Count; i++) {
				var comp = thingsOnTile[i].TryGetComp<CompWiredDetonationReceiver>();
				if (comp == null) continue;
				comp.RecieveSignal(delayOnThisTile);
			}
		}

		private void ConductSignalToNeighbours(int signalId, int sinalSteps) {
			if (parent.Map == null) throw new Exception("null map");
			var neighbours = GenAdj.CardinalDirectionsAround;
			for (var i = 0; i < neighbours.Length; i++) {
				var tileThings = parent.Map.thingGrid.ThingsListAtFast(neighbours[i] + parent.Position);
				for (int j = 0; j < tileThings.Count; j++) {
					var comp = tileThings[j].TryGetComp<CompWiredDetonationTransmitter>();
					if (comp == null) continue;
					comp.RecieveSignal(signalId, sinalSteps+1);
				}
			}
		}
	}
}