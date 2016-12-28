using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	/* 
	 * Calls a colonist to find a component and perform some work on a detonator table to unlock the usage of channels.
	 */
	public class JobDriver_InstallChannelsComponent : JobDriver {
		public const string JobDefName = "JobDef_InstallChannelsComponent";
		private const TargetIndex TableInd = TargetIndex.A;
		private const TargetIndex ComponentInd = TargetIndex.B;
		private const int InstallWorkAmount = 1500;

		private float workLeft;

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref workLeft, "workLeft", 0f);
		}

		protected override IEnumerable<Toil> MakeNewToils() {
			AddFailCondition(JobHasFailed);
			var table = TargetThingA as Building_DetonatorTable;
			if(table == null) yield break;
			yield return Toils_Reserve.Reserve(TableInd);
			yield return Toils_Reserve.Reserve(ComponentInd);
			yield return Toils_Goto.GotoCell(ComponentInd, PathEndMode.Touch);
			yield return Toils_Haul.StartCarryThing(ComponentInd);
			yield return Toils_Goto.GotoCell(TableInd, PathEndMode.InteractionCell);
			yield return Toils_Haul.PlaceHauledThingInCell(TableInd, null, false);
			var toil = new Toil {
				initAction = delegate {
					workLeft = InstallWorkAmount;
				},
				tickAction = delegate {
					var statValue = GetActor().GetStatValue(StatDefOf.ConstructionSpeed);
					workLeft -= statValue;
					if (workLeft > 0) return;
					if (table.WantChannelsComponent) {
						table.InstallChannelsComponent();
					}
					Map.reservationManager.Release(TargetThingB, GetActor());
					TargetThingB.Destroy();
					ReadyForNextToil();
				}
			};
			toil.WithEffect(EffecterDefOf.ConstructMetal, TableInd);
			toil.WithProgressBar(TargetIndex.A, () => 1f - workLeft / InstallWorkAmount);
			toil.defaultCompleteMode = ToilCompleteMode.Never;
			yield return toil;
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
