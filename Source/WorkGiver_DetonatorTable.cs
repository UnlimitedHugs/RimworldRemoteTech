using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
/**
 * Used to trigger remote explosives
 */
namespace RemoteExplosives {
	public class WorkGiver_DetonatorTable : WorkGiver_Scanner {
		
		private const PathEndMode pathEndMode = PathEndMode.InteractionCell;

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
				&& table.WantsDetonation()
				&& pawn.CanReserveAndReach(t, pathEndMode, Danger.Some);

			return status;
		}

		public override Job JobOnThing(Pawn pawn, Thing t) {
			var table = t as Building_DetonatorTable;
			if(table == null) return null;
			if (table.WantsDetonation()) {
				var jobDef = DefDatabase<JobDef>.GetNamed(JobDriver_DetonateRemoteExplosives.JobDefName);
				return new Job(jobDef, t);
			}
			return null;
		}
	}
}
