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
			return !pawn.workSettings.WorkIsActive(RemoteExplosivesDefOf.Cleaning);
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			var designations = pawn.Map.designationManager.DesignationsOfDef(RemoteExplosivesUtility.DryOffDesigationDef);
			foreach (var designation in designations) {
				if(designation.target.Thing == null) continue;
				yield return designation.target.Thing;
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t) {
			var wire = t as Building_DetonatorWire;
			if (wire == null) return false;
			return wire.WantDrying && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly);
		}

		public override Job JobOnThing(Pawn pawn, Thing t) {
			var wire = t as Building_DetonatorWire;
			if (wire == null) return null;
			if (!wire.WantDrying) return null;
			var jobDef = DefDatabase<JobDef>.GetNamed(JobDriver_DryDetonatorWire.JobDefName);
			return new Job(jobDef, t);
		}
	}
}