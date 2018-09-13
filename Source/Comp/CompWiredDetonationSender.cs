using System;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Transmits a new detonation signal to CompWiredDetonationTransmitter comps on the same tile. 
	/// </summary>
	public class CompWiredDetonationSender : CompDetonationGridNode {
		public void SendNewSignal() {
			if (parent.Map == null) throw new Exception("null map");
			var thingsOnTile = parent.Map.thingGrid.ThingsListAtFast(parent.Position);
			for (var i = 0; i < thingsOnTile.Count; i++) {
				var comp = thingsOnTile[i].TryGetComp<CompWiredDetonationTransmitter>();
				if (comp == null) continue;
				comp.ReceiveSignal(Rand.Int, 0);
			}
		}

		public override void PrintForDetonationGrid(SectionLayer layer) {
			PrintEndpoint(layer);
		}
	}
}