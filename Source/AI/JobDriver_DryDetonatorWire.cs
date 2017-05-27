using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace RemoteExplosives {
	/*
	 * Calls a colonist to a marked detonation wire to dry it off
	 */
	public class JobDriver_DryDetonatorWire : JobDriver {

		protected override IEnumerable<Toil> MakeNewToils() {
			AddFailCondition(JobHasFailed);
			var wire = TargetThingA as Building_DetonatorWire;
			if(wire == null) yield break;
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
			var jobDuration = wire.DryOffJobDuration;
			yield return Toils_General.Wait(jobDuration).WithEffect(EffecterDefOf.Clean, TargetIndex.A).WithProgressBarToilDelay(TargetIndex.A, jobDuration);
			yield return new Toil {
				initAction = () => {
					if (wire.WantDrying) {
						wire.DryOff();
					}
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			yield return Toils_Reserve.Release(TargetIndex.A);
		}

		private bool JobHasFailed() {
			var wire = TargetThingA as Building_DetonatorWire;
			return TargetThingA == null || TargetThingA.Destroyed || wire == null || !wire.WantDrying;
		}
	}
}