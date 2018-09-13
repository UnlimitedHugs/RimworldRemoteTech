using Verse;

namespace RemoteTech {
	/// <summary>
	/// Allows the wire crossing to be placed over wires and automatically deconstruct them
	/// </summary>
	public class PlaceWorker_DetWireReplacer : PlaceWorker {
		public override void PostPlace(Map map, BuildableDef def, IntVec3 loc, Rot4 rot) {
			foreach (var thing in map.thingGrid.ThingsAt(loc)) {
				if (thing.def != null && thing.def.HasComp(typeof(CompWiredDetonationTransmitter))) {
					thing.Destroy(DestroyMode.Deconstruct);
				}
			}
		}
	}
}