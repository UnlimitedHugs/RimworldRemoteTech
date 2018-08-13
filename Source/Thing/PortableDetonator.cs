using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	/* 
	 * An apparel item to be worn by a colonist in the accessory (shield) slot.
	 * Provides the functionality of a detonator table with a limited number of uses.
	 */
	public class PortableDetonator : Apparel {
		private const int ActivationCooldownTicks = 30;

		private static readonly string DetonateButtonLabel = "PortableDetonator_detonate_label".Translate();
		private static readonly string DetonateButtonDesc = "PortableDetonator_detonate_desc".Translate();
		private static readonly string NumUsesLeftInspectMessage = "PortableDetonator_detonate_uses".Translate();
		private static readonly string DetonatorBrokeMessage = "PortableDetonator_broke_msg".Translate();

		private bool rangeOverlayVisible;
		private int lastActivationTick; // prevents unintended double activations

		private int numUsesLeft;

		public override void PostMake() {
			base.PostMake();
			numUsesLeft = MaxNumUses;
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref numUsesLeft, "numUsesLeft");
		}

		public override void DrawWornExtras() {
			base.DrawWornExtras();
			DrawRangeOverlay();
		}

		public override string GetInspectString() {
			return string.Format(NumUsesLeftInspectMessage, numUsesLeft);
		}

		public override IEnumerable<Gizmo> GetWornGizmos() {
			yield return new Command_MouseOverDetector {
				action = OnGizmoActivation,
				mouseOverCallback = OnMouseOverGizmo,
				icon = Resources.Textures.UIDetonatorPortable,
				defaultLabel = DetonateButtonLabel,
				defaultDesc = DetonateButtonDesc + "\n" + GetInspectString(),
				hotKey = Resources.KeyBinging.rxPortableDetonatorDetonate
			};
		}

		private int SignalRange {
			get { return Mathf.RoundToInt(this.GetStatValue(Resources.Stat.rxPortableDetonatorRange)); }
		}

		private int MaxNumUses {
			get { return Mathf.RoundToInt(this.GetStatValue(Resources.Stat.rxPortableDetonatorNumUses)); }
		}

		private void DrawRangeOverlay() {
			if (!rangeOverlayVisible) return;
			rangeOverlayVisible = false;
			if (SignalRange <= GenRadial.MaxRadialPatternRadius) {
				GenDraw.DrawRadiusRing(Wearer.Position, SignalRange);
			}
		}

		private void OnMouseOverGizmo() {
			rangeOverlayVisible = true;
		}

		private void OnGizmoActivation() {
			if (lastActivationTick + ActivationCooldownTicks>=Find.TickManager.TicksGame) return;
			lastActivationTick = Find.TickManager.TicksGame;
			SoundDefOf.FlickSwitch.PlayOneShot(Wearer);

			RemoteExplosivesUtility.LightArmedExplosivesInNetworkRange(this, 1);
			
			numUsesLeft--;
			if (numUsesLeft <= 0) {
				Destroy(DestroyMode.KillFinalize);
				Messages.Message(DetonatorBrokeMessage, new TargetInfo(Wearer), MessageTypeDefOf.NeutralEvent);
			}
		}


	}
}