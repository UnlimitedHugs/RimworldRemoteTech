using System;
using Verse;

namespace RemoteExplosives {
	public class PlaceWorker_DetonatorWire : PlaceWorker {
		private readonly Type compType = typeof (CompWiredDetonationTransmitter);
		
		public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot) {
			var thingList = loc.GetThingList();
			for (var i = 0; i < thingList.Count; i++) {
				var thing = thingList[i];
				if (thing.def == null) return false;
				if (thing.def.HasComp(compType)) {
					return false;
				}
				if (thing.def.entityDefToBuild != null) {
					var thingDef = thingList[i].def.entityDefToBuild as ThingDef;
					if (thingDef != null && thingDef.HasComp(compType)) {
						return false;
					}
				}
			}
			return true;
		}
	}
}
