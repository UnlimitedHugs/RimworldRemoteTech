using System.Collections.Generic;
using HugsLib.Utils;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	/*
	 * Calls a colonist to transform a foam wall into one of its smoothed variants
	 */
	public class JobDriver_SmoothFoamWall : JobDriver {
		private float workLeft;

		public override bool TryMakePreToilReservations(bool errorOnFailed) {
			return pawn.Reserve(job.targetA, job);
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref workLeft, "workLeft");
		}

		protected override IEnumerable<Toil> MakeNewToils() {
			var wall = TargetThingA as Building_FoamWall;
			if (wall == null) yield break;
			this.FailOn(() => !wall.Spawned || !wall.HasDesignation(Resources.Designation.rxFoamWallSmooth));
			yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
			var doWork = new Toil {
				initAction = delegate {
					workLeft = wall.SmoothWorkAmount;
				}
			};
			doWork.tickAction = delegate {
				float workSpeed = doWork.actor.GetStatValue(StatDefOf.SmoothingSpeed);
				workLeft -= workSpeed;
				if (doWork.actor.skills != null) {
					doWork.actor.skills.Learn(SkillDefOf.Construction, 0.11f);
				}
				if (workLeft <= 0f) {
					wall.ToggleDesignation(Resources.Designation.rxFoamWallSmooth, false);
					wall.ApplySmoothing();
					EndJobWith(JobCondition.Succeeded);
				}
			};
			doWork.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			doWork.WithEffect(EffecterDefOf.ConstructDirt, TargetIndex.A);
			doWork.WithProgressBar(TargetIndex.A, () => 1f - workLeft / (float)wall.SmoothWorkAmount);
			doWork.defaultCompleteMode = ToilCompleteMode.Never;
			yield return doWork;
		}
	}
}