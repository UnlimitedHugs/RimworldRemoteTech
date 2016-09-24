using UnityEngine;
using Verse;

namespace RemoteExplosives {
	// Transmits the wired detonation signal to signal receiver comps and transmitter comps on adjacent tiles. 
	public class CompWiredDetonationTransmitter : ThingComp {
		private int lastSignalId;

		public void SendNewSignal() {
			RecieveSignal(Rand.Int, 0);
		}

		private void RecieveSignal(int signalId, int sinalSteps) {
			if (signalId == lastSignalId) return;
			lastSignalId = signalId;
			PassSignalToReceivers(sinalSteps);
			ConductSignalToNeighbours(signalId, sinalSteps);
		}

		private void PassSignalToReceivers(int sinalSteps) {
			var transmitterProps = props as CompProperties_WiredDetonationTransmitter;
			var delayPerTile = transmitterProps != null ? transmitterProps.signalDelayPerTile : 0;
			var delayOnThisTile = Mathf.RoundToInt(sinalSteps * delayPerTile);
			var thingsOnTile = Find.ThingGrid.ThingsListAtFast(parent.Position);
			for (var i = 0; i < thingsOnTile.Count; i++) {
				var comp = thingsOnTile[i].TryGetComp<CompWiredDetonationReceiver>();
				if (comp == null) continue;
				comp.RecieveSignal(delayOnThisTile);
			}
		}

		private void ConductSignalToNeighbours(int signalId, int sinalSteps) {
			var neighbours = GenAdj.CardinalDirectionsAround;
			for (var i = 0; i < neighbours.Length; i++) {
				var tileThings = Find.ThingGrid.ThingsListAtFast(neighbours[i] + parent.Position);
				for (int j = 0; j < tileThings.Count; j++) {
					var comp = tileThings[j].TryGetComp<CompWiredDetonationTransmitter>();
					if (comp == null) continue;
					comp.RecieveSignal(signalId, sinalSteps+1);
				}
			}
		}
	}
}