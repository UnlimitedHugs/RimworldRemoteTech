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
	[StaticConstructorOnStartup]
	public class PortableDetonator : Apparel {
		private const int ActivationCooldownTicks = 30;

		private static readonly Texture2D UITex_PortableDetonator = ContentFinder<Texture2D>.Get("UIDetonatorPortable");
		private static readonly string DetonateButtonLabel = "PortableDetonator_detonate_label".Translate();
		private static readonly string DetonateButtonDesc = "PortableDetonator_detonate_desc".Translate();
		private static readonly string NumUsesLeftInspectMessage = "PortableDetonator_detonate_uses".Translate();
		private static readonly string DetonatorBrokeMessage = "PortableDetonator_broke_msg".Translate();
		private static readonly StatDef detonatorRangeStat = DefDatabase<StatDef>.GetNamed("PortableDetonatorRange");
		private static readonly StatDef detonatorNumUsesStat = DefDatabase<StatDef>.GetNamed("PortableDetonatorNumUses");

		private bool rangeOverlayVisible;
		private int lastActivationTick; // prevents unintended double activations
		private bool justMade;

		private int numUsesLeft;

		public override void PostMake() {
			base.PostMake();
			justMade = true;
		}

		public override void SpawnSetup() {
			base.SpawnSetup();
			if (justMade) {
				justMade = false;
				numUsesLeft = MaxNumUses;
			}
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.LookValue(ref numUsesLeft, "numUsesLeft", 0);
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
				icon = UITex_PortableDetonator,
				defaultLabel = DetonateButtonLabel,
				defaultDesc = DetonateButtonDesc + "\n" + GetInspectString(),
				hotKey = KeyBindingDef.Named("PortableDetonatorDetonate")
			};
		}

		private int SignalRange {
			get { return Mathf.RoundToInt(this.GetStatValue(detonatorRangeStat)); }
		}

		private int MaxNumUses {
			get { return Mathf.RoundToInt(this.GetStatValue(detonatorNumUsesStat)); }
		}

		private void DrawRangeOverlay() {
			if (!rangeOverlayVisible) return;
			rangeOverlayVisible = false;
			if (SignalRange <= GenRadial.MaxRadialPatternRadius) {
				GenDraw.DrawRadiusRing(wearer.Position, SignalRange);
			}
		}

		private void OnMouseOverGizmo() {
			rangeOverlayVisible = true;
		}

		private void OnGizmoActivation() {
			if (lastActivationTick + ActivationCooldownTicks>=Find.TickManager.TicksGame) return;
			lastActivationTick = Find.TickManager.TicksGame;
			SoundDefOf.FlickSwitch.PlayOneShot(wearer.Position);

			RemoteExplosivesUtility.LightArmedExplosivesInRange(wearer.Position, SignalRange, RemoteExplosivesUtility.RemoteChannel.White);
			
			numUsesLeft--;
			if (numUsesLeft <= 0) {
				Destroy(DestroyMode.Kill);
				Messages.Message(DetonatorBrokeMessage, new TargetInfo(wearer), MessageSound.Negative);
			}
		}


	}
}