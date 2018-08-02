using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	/* 
	 * Gives JobDriver_InstallChannelsComponent job to colonists
	 */
	public class WorkGiver_DetonatorTableChannels : WorkGiver_Scanner {

		private const PathEndMode pathEndMode = PathEndMode.InteractionCell;
		private const int maxComponentSearchDist = 999;
		
		public override ThingRequest PotentialWorkThingRequest {
			get {
				return ThingRequest.ForDef(Resources.Thing.rxTableDetonator);
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var table = t as Building_DetonatorTable;
			if (table == null) return false;

			var status =
				!pawn.Dead
				&& !pawn.Downed
				&& !pawn.IsBurning()
				&& table.WantChannelsComponent
				&& pawn.CanReserveAndReach(t, pathEndMode, Danger.Some);

			if (status) {
				// also, find a haulable component
				if (FindInstallableComponent(pawn) == null) status = false;
			}
			return status;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var table = t as Building_DetonatorTable;
			if (table == null) return null;
			if (!table.WantChannelsComponent) return null;
			var component = FindInstallableComponent(pawn);
			if (component == null) return null;
			var jobDef = Resources.Job.rxInstallChannelsComponent;
			return new Job(jobDef, t, component) { count = 1 };
		}

		private Thing FindInstallableComponent(Pawn pawn) {
			bool SearchPredicate(Thing thing) => !thing.IsForbidden(pawn) && pawn.CanReserve(thing);
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ThingDefOf.ComponentIndustrial), PathEndMode.ClosestTouch, TraverseParms.For(pawn), maxComponentSearchDist, SearchPredicate);
		}

	}
}
