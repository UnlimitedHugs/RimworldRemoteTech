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
			var exposives = pawn.Map.designationManager.SpawnedDesignationsOfDef(Resources.Designation.RemoteExplosiveSwitch);
			foreach (var exposive in exposives) {
				yield return exposive.target.Thing;
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
			if (!(t is Building_RemoteExplosive)) return false;
			return
				!pawn.Dead
				&& !pawn.Downed
				&& !pawn.IsBurning()
				&& (t as Building_RemoteExplosive).WantsSwitch()
				&& pawn.CanReserveAndReach(t, pathEndMode, Danger.Deadly);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var jobDef = Resources.Job.SwitchRemoteExplosive;
			return new Job(jobDef, t);
		}
	}
}
