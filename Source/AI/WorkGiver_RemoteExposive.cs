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
			var explosives = pawn.Map.designationManager.SpawnedDesignationsOfDef(Resources.Designation.rxRemoteExplosiveSwitch);
			foreach (var explosive in explosives) {
				yield return explosive.target.Thing;
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
			if (!(t is ISwitchable)) return false;
			return
				!pawn.Dead
				&& !pawn.Downed
				&& !pawn.IsBurning()
				&& (t as ISwitchable).WantsSwitch()
				&& pawn.CanReserveAndReach(t, pathEndMode, Danger.Deadly);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var jobDef = Resources.Job.rxSwitchRemoteExplosives;
			return new Job(jobDef, t);
		}
	}
}
