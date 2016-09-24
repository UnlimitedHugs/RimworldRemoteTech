using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	// Calls a colonist to find a component and perform some work on a detonator table to unlock the usage of channels.
	public class JobDriver_InstallChannelsComponent : JobDriver {
		public const string JobDefName = "JobDef_InstallChannelsComponent";
		private const TargetIndex TableInd = TargetIndex.A;
		private const TargetIndex ComponentInd = TargetIndex.B;
		private const int InstallWorkDuration = 250;

		protected override IEnumerable<Toil> MakeNewToils() {
			AddFailCondition(JobHasFailed);
			var table = TargetThingA as Building_DetonatorTable;
			yield return Toils_Reserve.Reserve(TableInd);
			yield return Toils_Reserve.Reserve(ComponentInd);
			yield return Toils_Goto.GotoCell(ComponentInd, PathEndMode.Touch);
			yield return Toils_Haul.StartCarryThing(ComponentInd);
			yield return Toils_Goto.GotoCell(TableInd, PathEndMode.InteractionCell);
			yield return Toils_General.Wait(InstallWorkDuration).WithEffect(EffecterDefOf.ConstructMetal, TableInd).WithProgressBarToilDelay(ComponentInd, InstallWorkDuration);
			yield return Toils_Reserve.Release(ComponentInd);
			yield return new Toil {
				initAction = ()=>{
					if(table!=null && table.WantChannelsComponent) {
						table.InstallChannelsComponent();
					}
					TargetThingB.Destroy();
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
			yield return Toils_Reserve.Release(TableInd);
		}

		private bool JobHasFailed() {
			var table = TargetThingA as Building_DetonatorTable;
			var componentComps = TargetThingB as ThingWithComps;
			var forbidden = false;
			if(componentComps!=null && componentComps.GetComp<CompForbiddable>()!=null) {
				forbidden = componentComps.GetComp<CompForbiddable>().Forbidden;
			}
			if (TargetThingA.Destroyed || table == null || !table.WantChannelsComponent || TargetThingB == null || TargetThingB.Destroyed || forbidden) return true;
			return false;
		}
	}
}
