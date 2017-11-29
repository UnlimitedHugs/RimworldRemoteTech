using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	/* 
	 * Calls a colonist to a detonator to perform the detonation.
	 */
	public class JobDriver_DetonateExplosives : JobDriver {
		public override bool TryMakePreToilReservations() {
			return pawn.Reserve(job.targetA, job);
		}

		protected override IEnumerable<Toil> MakeNewToils(){
			var detonator = TargetThingA as IPawnDetonateable;
			if (detonator == null) yield break;
			var pathEndMode = detonator.UseInteractionCell ? PathEndMode.InteractionCell : PathEndMode.ClosestTouch;
			AddFailCondition(JobHasFailed);
			yield return Toils_Goto.GotoCell(TargetIndex.A, pathEndMode);
			yield return new Toil {
				initAction = ()=> detonator.DoDetonation(),
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}

		private bool JobHasFailed() {
			var detonator = TargetThingA as IPawnDetonateable;
			return detonator == null || ((Building)detonator).Destroyed || !detonator.WantsDetonation;
		}
	}
}
