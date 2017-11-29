using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	/* 
	 * Issues jobs on behalf of Building_FoamWall to do smoothing work on it when it is appropriately designated
	 */
	public class WorkGiver_FoamWall : WorkGiver_Scanner {
		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			var designations = pawn.Map.designationManager.SpawnedDesignationsOfDef(Resources.Designation.FoamWallSmooth);
			foreach (var designation in designations) {
				if(designation.target.Thing == null) continue;
				yield return designation.target.Thing;
			}
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var wall = t as Building_FoamWall;
			if (wall == null || !wall.Spawned || !pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly)) return null;
			return new Job(Resources.Job.SmoothFoamWall, t);
		}
	}
}