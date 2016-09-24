using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	// Gives JobDriver_DetonateExplosives job to colonists
	// Handles both the detonator table and the manual detonator
	public class WorkGiver_IPawnDetonateable : WorkGiver_Scanner {
		public override ThingRequest PotentialWorkThingRequest {
			get {
				return ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial);
			}
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn Pawn) {
			var buildings = Find.ListerBuildings.allBuildingsColonist;
			for (var i = 0; i < buildings.Count; i++) {
				var detonator = buildings[i] as IPawnDetonateable;
				if (detonator != null) yield return buildings[i];
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t) {
			var detonator = t as IPawnDetonateable;
			if (detonator == null) return false;

			var pathEndMode = detonator.UseInteractionCell ? PathEndMode.InteractionCell : PathEndMode.ClosestTouch;
			var status = 
				!pawn.Dead
				&& !pawn.Downed
				&& !pawn.IsBurning()
				&& detonator.WantsDetonation()
				&& pawn.CanReserveAndReach(t, pathEndMode, Danger.Some);

			return status;
		}

		public override Job JobOnThing(Pawn pawn, Thing t) {
			var detonator = t as IPawnDetonateable;
			if(detonator == null) return null;
			if (!detonator.WantsDetonation()) return null;
			var jobDef = DefDatabase<JobDef>.GetNamed(JobDriver_DetonateExplosives.JobDefName);
			return new Job(jobDef, t);
		}
	}
}
