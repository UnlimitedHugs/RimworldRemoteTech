using System.Collections.Generic;
using Verse.AI;

namespace RemoteExplosives {
	// Calls a colonist to flick a remote explosive.
	// This includes both setting the armed state and the channel.
	public class JobDriver_SwitchRemoteExplosive : JobDriver {
		public static string JobDefName = "JobDef_SwitchRemoteExplosive";

		protected override IEnumerable<Toil> MakeNewToils() {
			AddFailCondition(JobHasFailed);
			var explosive = TargetThingA as Building_RemoteExplosive;
			if (explosive == null) yield break;
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.InteractionCell);
			yield return new Toil {
				initAction = () => explosive.DoSwitch(),
				defaultCompleteMode = ToilCompleteMode.Instant,
			};

			yield return Toils_Reserve.Release(TargetIndex.A);
		}

		private bool JobHasFailed() {
			var explosive = TargetThingA as Building_RemoteExplosive;
			return explosive == null || explosive.Destroyed || !explosive.WantsSwitch();
		}
	}
}
