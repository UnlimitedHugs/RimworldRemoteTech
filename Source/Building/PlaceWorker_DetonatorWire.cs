using System;
using Verse;

namespace RemoteExplosives {
	/*
	 * Allows detonator wire to be placed under existing structures
	 */
	public class PlaceWorker_DetonatorWire : PlaceWorker {
		private readonly Type compTypeTransmitter = typeof (CompWiredDetonationTransmitter);
		private readonly Type compTypeCrossing = typeof (CompWiredDetonationCrossing);
		
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null) {
			var thingList = loc.GetThingList(map);
			for (var i = 0; i < thingList.Count; i++) {
				var thingOnTile = thingList[i];
				if (thingOnTile.def == null) return false;
				if (thingOnTile.def.HasComp(compTypeTransmitter) || thingOnTile.def.HasComp(compTypeCrossing)) {
					return false;
				}
				if (thingOnTile.def.entityDefToBuild != null) {
					var thingDef = thingList[i].def.entityDefToBuild as ThingDef;
					if (thingDef != null && (thingDef.HasComp(compTypeTransmitter) ||  thingOnTile.def.HasComp(compTypeCrossing))) {
						return false;
					}
				}
			}
			return true;
		}
	}
}
