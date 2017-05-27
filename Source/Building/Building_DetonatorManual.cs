using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	/* 
	 * Initates a detonation signal carried by wire when triggered by a colonist.
	 */
	public class Building_DetonatorManual : Building, IGraphicVariantProvider, IPawnDetonateable {
		private static readonly string DetonateButtonLabel = "DetonatorManual_detonate_label".Translate();
		private static readonly string DetonateButtonDesc = "DetonatorManual_detonate_desc".Translate();

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
		private bool wantDetonation;

		public bool UseInteractionCell {
			get { return false; }
		}

		public bool WantsDetonation {
			get { return wantDetonation; }
			set { wantDetonation = value; }
		}

		public void DoDetonation() {
			wantDetonation = false;
			currentVariant = VisualVariant.PlungerDown;
			plungerExpireTime = Time.realtimeSinceStartup + PlungerDownTime;
			Resources.Sound.RemoteDetonatorLever.PlayOneShot(new TargetInfo(Position, Map));
			var transmitterComp = GetComp<CompWiredDetonationSender>();
			if(transmitterComp != null) transmitterComp.SendNewSignal();
		}
		
		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref wantDetonation, "wantDetonation", false);
		}

		public override IEnumerable<Gizmo> GetGizmos() {
			var detonateGizmo = new Command_Toggle {
				toggleAction = DetonateGizmoAction,
				isActive = () => wantDetonation,
				icon = Resources.Textures.UITrigger,
				defaultLabel = DetonateButtonLabel,
				defaultDesc = DetonateButtonDesc,
				hotKey = Resources.KeyBinging.RemoteTableDetonate
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
			var opt = RemoteExplosivesUtility.TryMakeDetonatorFloatMenuOption(selPawn, this);
			if (opt != null) yield return opt;

			foreach (var option in base.GetFloatMenuOptions(selPawn)) {
				yield return option;
			}
		}
	}
}