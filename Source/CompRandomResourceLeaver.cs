using Verse;

namespace RemoteExplosives {
	/* 
	 * Drops a random amount of a certain item on destruction.
	 */
	public class CompRandomResourceLeaver : ThingComp {
		public override void PostDestroy(DestroyMode mode, bool wasSpawned) {
			base.PostDestroy(mode, wasSpawned);
			var leaverProps = props as CompProperties_RandomResourceLeaver;
			if (leaverProps == null || leaverProps.thingDef == null) return;
			if (mode != leaverProps.requiredDestroyMode) return;
			var amount = leaverProps.amountRange.RandomInRange;
			if(amount <= 0) return;
			var drop = ThingMaker.MakeThing(leaverProps.thingDef);
			drop.stackCount = amount;
			GenPlace.TryPlaceThing(drop, parent.Position, ThingPlaceMode.Near);
		}
	}
}
