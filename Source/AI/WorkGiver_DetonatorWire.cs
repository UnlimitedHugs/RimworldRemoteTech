using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	/* 
	 * Issues jobs on behalf of Building_DetonatorWire to dry it off when it is appropriately designated
	 */
	public class WorkGiver_DetonatorWire : WorkGiver_Scanner {
		public override PathEndMode PathEndMode {
			get { return PathEndMode.ClosestTouch; }
		}

		public override bool ShouldSkip(Pawn pawn) {
			return !pawn.workSettings.WorkIsActive(Resources.WorkType.Cleaning);
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			var designations = pawn.Map.designationManager.SpawnedDesignationsOfDef(Resources.Designation.DetonatorWireDryOff);
			foreach (var designation in designations) {
				if(designation.target.Thing == null) continue;
				yield return designation.target.Thing;
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var wire = t as Building_DetonatorWire;
			if (wire == null) return false;
			return wire.WantDrying && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly);
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var wire = t as Building_DetonatorWire;
			if (wire == null) return null;
			if (!wire.WantDrying) return null;
			var jobDef = Resources.Job.DryDetonatorWire;
			return new Job(jobDef, t);
		}
	}
}