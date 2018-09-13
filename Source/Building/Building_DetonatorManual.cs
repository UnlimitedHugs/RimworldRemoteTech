using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteTech {
	/// <summary>
	/// Initiates a detonation signal carried by wire when triggered by a colonist.
	/// </summary>
	public class Building_DetonatorManual : Building, IGraphicVariantProvider, IPawnDetonateable, IRedButtonFeverTarget {
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
			Resources.Sound.rxDetonatorLever.PlayOneShot(new TargetInfo(Position, Map));
			var transmitterComp = GetComp<CompWiredDetonationSender>();
			if(transmitterComp != null) transmitterComp.SendNewSignal();
		}
		
		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref wantDetonation, "wantDetonation");
		}

		public override IEnumerable<Gizmo> GetGizmos() {
			Command detonate;
			if (CanDetonateImmediately()) {
				detonate = new Command_Action {
					action = DoDetonation,
					defaultLabel = "Detonator_detonateNow_label".Translate(),
				};
			} else {
				detonate = new Command_Toggle {
					toggleAction = DetonateToggleAction,
					isActive = () => wantDetonation,
					defaultLabel = "DetonatorManual_detonate_label".Translate(),
				};
			}
			detonate.icon = Resources.Textures.rxUIDetonateManual;
			detonate.defaultDesc = "DetonatorManual_detonate_desc".Translate();
			detonate.hotKey = Resources.KeyBinging.rxRemoteTableDetonate;
			yield return detonate;

			foreach (var g in base.GetGizmos()) {
				yield return g;
			}
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
			var opt = RemoteTechUtility.TryMakeDetonatorFloatMenuOption(selPawn, this);
			if (opt != null) yield return opt;

			foreach (var option in base.GetFloatMenuOptions(selPawn)) {
				yield return option;
			}
		}

		public bool RedButtonFeverCanInteract {
			get { return true; }
		}

		public void RedButtonFeverDoInteraction(Pawn p) {
			DoDetonation();
		}

		private void DetonateToggleAction() {
			wantDetonation = !wantDetonation;
		}

		private bool CanDetonateImmediately() {
			// a drafted pawn in any of the adjacent cells allows for immediate detonation
			for (var i = 0; i < GenAdj.AdjacentCellsAround.Length; i++) {
				var manningPawn = (Position + GenAdj.AdjacentCellsAround[i]).GetFirstPawn(Map);
				if (manningPawn != null && manningPawn.Drafted) return true;
			}
			return false;
		}
	}
}