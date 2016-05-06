using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	public class WorkGiver_DetonatorTable : WorkGiver_Scanner {
		
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
				&& (table.WantsDetonation() || table.WantChannelsComponent)
				&& pawn.CanReserveAndReach(t, pathEndMode, Danger.Some);

			if (status && table.WantChannelsComponent) {
				// also, find a haulable component
				if (FindInstallableComponent(pawn) == null) status = false;
			}
			return status;
		}

		public override Job JobOnThing(Pawn pawn, Thing t) {
			var table = t as Building_DetonatorTable;
			if(table == null) return null;
			JobDef jobDef;
			if (table.WantsDetonation()) {
				jobDef = DefDatabase<JobDef>.GetNamed(JobDriver_DetonateRemoteExplosives.JobDefName);
				return new Job(jobDef, t);
			} else if(table.WantChannelsComponent){
				jobDef = DefDatabase<JobDef>.GetNamed(JobDriver_InstallChannelsComponent.JobDefName);
				var component = FindInstallableComponent(pawn);
				return new Job(jobDef, t, component) { maxNumToCarry = 1 };
			}
			return null;
		}

		private Thing FindInstallableComponent(Pawn pawn) {
			Predicate<Thing> searchPredicate = thing => !thing.IsForbidden(pawn.Faction) && pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Some);
			return GenClosest.ClosestThingReachable(pawn.Position, ThingRequest.ForDef(ThingDefOf.Components), PathEndMode.ClosestTouch, TraverseParms.For(TraverseMode.ByPawn), maxComponentSearchDist, searchPredicate);
		}
	}
}
