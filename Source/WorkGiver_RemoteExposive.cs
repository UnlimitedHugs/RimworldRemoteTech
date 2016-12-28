using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	/*
	 * Gives JobDriver_SwitchRemoteExplosive job to colonists
	 */
	public class WorkGiver_RemoteExplosive : WorkGiver_Scanner {
		
		private const PathEndMode pathEndMode = PathEndMode.Touch;
		
		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			var exposives = pawn.Map.designationManager.DesignationsOfDef(RemoteExplosivesUtility.SwitchDesigationDef);
			foreach (var exposive in exposives) {
				yield return exposive.target.Thing;
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t) {
			if (!(t is Building_RemoteExplosive)) return false;
			return
				!pawn.Dead
				&& !pawn.Downed
				&& !pawn.IsBurning()
				&& (t as Building_RemoteExplosive).WantsSwitch()
				&& pawn.CanReserveAndReach(t, pathEndMode, Danger.Deadly);
		}

		public override Job JobOnThing(Pawn pawn, Thing t) {
			var jobDef = DefDatabase<JobDef>.GetNamed(JobDriver_SwitchRemoteExplosive.JobDefName);
			return new Job(jobDef, t);
		}
	}
}
