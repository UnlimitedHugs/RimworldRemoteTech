using Verse;

namespace RemoteExplosives {
	// Drops a random amount of a certain item on death.
	public class CompRandomResourceLeaver : ThingComp {
		public override void PostDestroy(DestroyMode mode, bool wasSpawned) {
			base.PostDestroy(mode, wasSpawned);
			if(mode!=DestroyMode.Kill) return;
			var leaverProps = props as CompRandomResourceLeaverProperties;
			if (leaverProps == null || leaverProps.thingDef == null) return;
			var drop = ThingMaker.MakeThing(leaverProps.thingDef);
			drop.stackCount = leaverProps.amount.RandomInRange;
			GenPlace.TryPlaceThing(drop, parent.Position, ThingPlaceMode.Near);
		}
	}
}
