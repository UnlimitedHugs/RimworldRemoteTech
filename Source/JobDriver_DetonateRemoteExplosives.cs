using System.Collections.Generic;
using Verse.AI;

namespace RemoteExplosives {
	// Calls a colonist to a detonator table to perform the detonation.
	public class JobDriver_DetonateRemoteExplosives : JobDriver {
		public static string JobDefName = "JobDef_DetonateRemoteExplosives";

		protected override IEnumerable<Toil> MakeNewToils(){
			AddFailCondition(JobHasFailed);
			var table = TargetThingA as Building_DetonatorTable;
			if(table == null) yield break;
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);
			yield return new Toil {
				initAction = ()=> table.DoDetonation(),
				defaultCompleteMode = ToilCompleteMode.Instant,
			};
			
			yield return Toils_Reserve.Release(TargetIndex.A);
		}

		private bool JobHasFailed() {
			var table = TargetThingA as Building_DetonatorTable;
			return table == null || table.Destroyed || !table.WantsDetonation();
		}
	}
}
