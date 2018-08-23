using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	/*
	 * Gives JobDriver_SwitchThing job to colonists
	 */
	public class WorkGiver_SwitchableThing : WorkGiver_Scanner {
		
		private const PathEndMode pathEndMode = PathEndMode.Touch;
		
		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			var things = pawn.Map.designationManager.SpawnedDesignationsOfDef(Resources.Designation.rxSwitchThing);
			foreach (var des in things) {
				yield return des.target.Thing;
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
			if (!t.WantsSwitching()) return false;
			return
				!pawn.Dead
				&& !pawn.Downed
				&& !pawn.IsBurning()
				&& pawn.CanReserveAndReach(t, pathEndMode, Danger.Deadly);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var jobDef = Resources.Job.rxSwitchThing;
			return new Job(jobDef, t);
		}
	}
}
