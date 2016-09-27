using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	/* 
	 * Issues jobs on behalf of Building_DetonatorCord to dry it off when it is appropriately designated
	 */
	public class WorkGiver_DetonatorCord : WorkGiver_Scanner {
		public override PathEndMode PathEndMode {
			get { return PathEndMode.ClosestTouch; }
		}

		public override bool ShouldSkip(Pawn pawn) {
			return !pawn.workSettings.WorkIsActive(WorkTypeDefOf.Cleaning);
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn Pawn) {
			var designations = Find.DesignationManager.DesignationsOfDef(RemoteExplosivesUtility.DryOffDesigationDef);
			foreach (var designation in designations) {
				if(designation.target.Thing == null) continue;
				yield return designation.target.Thing;
			}
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t) {
			var cord = t as Building_DetonatorCord;
			if (cord == null) return false;
			return cord.WantDrying;
		}

		public override Job JobOnThing(Pawn pawn, Thing t) {
			var cord = t as Building_DetonatorCord;
			if (cord == null) return null;
			if (!cord.WantDrying) return null;
			var jobDef = DefDatabase<JobDef>.GetNamed(JobDriver_DryDetonationCord.JobDefName);
			return new Job(jobDef, t);
		}
	}
}