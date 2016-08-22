using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	// Gives JobDriver_InstallChannelsComponent job to colonists
	public class WorkGiver_DetonatorTableChannels : WorkGiver_Scanner {

		private const PathEndMode pathEndMode = PathEndMode.InteractionCell;
		private const int maxComponentSearchDist = 999;

		private readonly ThingDef detonatorDef = ThingDef.Named("TableDetonator");

		public override ThingRequest PotentialWorkThingRequest {
			get {
				return ThingRequest.ForDef(detonatorDef);
			}
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn Pawn) {
			var detonators = Find.ListerBuildings.AllBuildingsColonistOfClass<Building_DetonatorTable>();
			foreach (var detonatorTable in detonators) {
				yield return detonatorTable;
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t) {
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

		public override Job JobOnThing(Pawn pawn, Thing t) {
			var table = t as Building_DetonatorTable;
			if (table == null) return null;
			if (table.WantChannelsComponent) {
				var jobDef = DefDatabase<JobDef>.GetNamed(JobDriver_InstallChannelsComponent.JobDefName);
				var component = FindInstallableComponent(pawn);
				return new Job(jobDef, t, component) { maxNumToCarry = 1 };
			}
			return null;
		}

		private Thing FindInstallableComponent(Pawn pawn) {
			Predicate<Thing> searchPredicate = thing => !thing.IsForbidden(pawn) && pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Some);
			Thing component = null;
			try {
				component = GenClosest.ClosestThingReachable(pawn.Position, ThingRequest.ForDef(ThingDefOf.Component), PathEndMode.ClosestTouch,
																 TraverseParms.For(TraverseMode.ByPawn, Danger.Some), maxComponentSearchDist, searchPredicate);
			} catch (NullReferenceException) {
				// there seems to be a problem with the vanilla search algorithm right now (at PawnUtility.GetAvoidGrid)
				// just fail the search should it come up
			}
			return component;
		}
	}
}
