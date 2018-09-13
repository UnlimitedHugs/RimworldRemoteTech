using Verse;

namespace RemoteTech {
	/// <summary>
	/// Drops a random amount of a certain item on destruction.
	/// </summary>
	public class CompRandomResourceLeaver : ThingComp {
		public override void PostDestroy(DestroyMode mode, Map map) {
			base.PostDestroy(mode, map);
			var leaverProps = props as CompProperties_RandomResourceLeaver;
			if (leaverProps?.thingDef == null) return;
			if (mode != leaverProps.requiredDestroyMode) return;
			var amount = leaverProps.amountRange.RandomInRange;
			if(amount <= 0) return;
			var drop = ThingMaker.MakeThing(leaverProps.thingDef);
			drop.stackCount = amount;
			GenPlace.TryPlaceThing(drop, parent.Position, map, ThingPlaceMode.Near);
		}
	}
}
