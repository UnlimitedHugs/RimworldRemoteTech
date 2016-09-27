using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RemoteExplosives {
	/* 
	 * Initates a detonation signal carried by wire when triggered by a colonist.
	 */
	[StaticConstructorOnStartup]
	public class Building_DetonatorManual : Building, IGraphicVariantProvider, IPawnDetonateable {
		private static readonly Texture2D UITex_Detonate = ContentFinder<Texture2D>.Get("UItrigger");
		private static readonly string DetonateButtonLabel = "DetonatorManual_detonate_label".Translate();
		private static readonly string DetonateButtonDesc = "DetonatorManual_detonate_desc".Translate();
		private static readonly SoundDef PlungerCycleSound = SoundDef.Named("RemoteDetonatorLever");

		private enum VisualVariant {
			PlungerUp = 0,
			PlungerDown = 1
		}

		private const float PlungerDownTime = 0.2f;

		private VisualVariant currentVariant;
		public int GraphicVariant {
			get { return (int) currentVariant; }
		}

		private float plungerExpireTime;
		private Pawn lastSeenFloatMenuPawn;
		private bool wantDetonation;

		public bool UseInteractionCell {
			get { return false; }
		}

		public bool WantsDetonation() {
			return wantDetonation;
		}

		public void DoDetonation() {
			wantDetonation = false;
			currentVariant = VisualVariant.PlungerDown;
			plungerExpireTime = Time.realtimeSinceStartup + PlungerDownTime;
			PlungerCycleSound.PlayOneShot(Position);
			var transmitterComp = GetComp<CompWiredDetonationSender>();
			if(transmitterComp != null) transmitterComp.SendNewSignal();
		}
		
		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref wantDetonation, "wantDetonation", false);
		}

		public override IEnumerable<Gizmo> GetGizmos() {
			var detonateGizmo = new Command_Toggle {
				toggleAction = DetonateGizmoAction,
				isActive = () => wantDetonation,
				icon = UITex_Detonate,
				defaultLabel = DetonateButtonLabel,
				defaultDesc = DetonateButtonDesc,
				hotKey = KeyBindingDef.Named("RemoteTableDetonate")
			};
			yield return detonateGizmo;

			foreach (var g in base.GetGizmos()) {
				yield return g;
			}
		}

		private void DetonateGizmoAction() {
			wantDetonation = !wantDetonation;
		}

		public override void Draw() {
			if (plungerExpireTime < Time.realtimeSinceStartup) {
				currentVariant = VisualVariant.PlungerUp;
				plungerExpireTime = 0;
			}
			base.Draw();
		}

		// quick detonation option for drafted pawns
		public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn) {
			lastSeenFloatMenuPawn = selPawn;
			var opt = RemoteExplosivesUtility.TryMakeDetonatorFloatMenuOption(selPawn, this, FloatMenuDetonateNowAction);
			if (opt != null) yield return opt;

			foreach (var option in base.GetFloatMenuOptions(selPawn)) {
				yield return option;
			}
		}

		private void FloatMenuDetonateNowAction() {
			if (lastSeenFloatMenuPawn == null) return;
			if (!wantDetonation) wantDetonation = true;
			var job = new Job(DefDatabase<JobDef>.GetNamed(JobDriver_DetonateExplosives.JobDefName), this);
			lastSeenFloatMenuPawn.drafter.TakeOrderedJob(job);
		}
	}
}