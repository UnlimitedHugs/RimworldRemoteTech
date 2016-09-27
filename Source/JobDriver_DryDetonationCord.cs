using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	/*
	 * Calls a colonist to a marked detonation cord to dry it off
	 */
	public class JobDriver_DryDetonationCord : JobDriver {
		public const string JobDefName = "JobDef_DryDetonationCord";
		private const string CleanEffecterDefName = "Clean";

		protected override IEnumerable<Toil> MakeNewToils() {
			AddFailCondition(JobHasFailed);
			var cord = TargetThingA as Building_DetonatorCord;
			if(cord == null) yield break;
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
			var jobDuration = cord.DryOffJobDuration;
			yield return Toils_General.Wait(jobDuration).WithEffect(EffecterDef.Named(CleanEffecterDefName), TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A, jobDuration);
			yield return new Toil {
				initAction = () => {
					if (cord.WantDrying) {
						cord.DryOff();
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			yield return Toils_Reserve.Release(TargetIndex.A);
		}

		private bool JobHasFailed() {
			var cord = TargetThingA as Building_DetonatorCord;
			return TargetThingA == null || TargetThingA.Destroyed || cord == null || !cord.WantDrying;
		}
	}
}