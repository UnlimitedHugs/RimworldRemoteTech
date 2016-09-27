using Verse;

namespace RemoteExplosives {
	/*
	 * Transmits a new detonation signal to CompWiredDetonationTransmitter comps on the same tile. 
	 */
	public class CompWiredDetonationSender : CompDetonationGridNode {
		public void SendNewSignal() {
			var thingsOnTile = Find.ThingGrid.ThingsListAtFast(parent.Position);
			for (var i = 0; i < thingsOnTile.Count; i++) {
				var comp = thingsOnTile[i].TryGetComp<CompWiredDetonationTransmitter>();
				if (comp == null) continue;
				comp.RecieveSignal(Rand.Int, 0);
			}
		}

		public override void PrintForDetonationGrid(SectionLayer layer) {
			PrintEndpoint(layer);
		}
	}
}