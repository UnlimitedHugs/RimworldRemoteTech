using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteTech {
	/* 
	 * An apparel item to be worn by a colonist in the accessory (shield) slot.
	 * Provides the functionality of a detonator table with a limited number of uses.
	 */
	public class PortableDetonator : Apparel {
		private const int ActivationCooldownTicks = 30;
		private const string ChannelsUpgradeId = "ChannelsBasic";

		private static readonly string DetonateButtonDesc = "PortableDetonator_detonateChannel_desc".Translate();
		private static readonly string NumUsesLeftInspectMessage = "PortableDetonator_detonate_uses".Translate();

		private bool rangeOverlayVisible;
		private int lastActivationTick; // prevents unintended double activations
		private CompWirelessDetonationGridNode node;
		private CompUpgrade channelsUpgrade;
		private CompChannelSelector channelsComp;

		// saved
		private int numUsesLeft = -1;

		private int MaxNumUses {
			get { return Mathf.RoundToInt(this.GetStatValue(Resources.Stat.rxPortableDetonatorNumUses)); }
		}

		private int NumUsesLeft {
			get { return numUsesLeft < 0 ? numUsesLeft = MaxNumUses : numUsesLeft; }
			set { numUsesLeft = value; }
		}

		private int Channel {
			get { return channelsComp != null ? channelsComp.Channel : RemoteTechUtility.DefaultChannel; }
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			GetCompRefs();
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref numUsesLeft, "numUsesLeft", -1);
			if (Scribe.mode == LoadSaveMode.PostLoadInit) {
				// apparel does not get SpawnSetup calls, we can init here
				GetCompRefs();
			}
		}

		public override void DrawWornExtras() {
			base.DrawWornExtras();
			DrawRangeOverlay();
		}

		public override string GetInspectString() {
			return string.Format(NumUsesLeftInspectMessage, NumUsesLeft);
		}

		public override IEnumerable<Gizmo> GetWornGizmos() {
			var wearer = Wearer;
			yield return new Command_MouseOverDetector {
				action = OnGizmoActivation,
				mouseOverCallback = OnMouseOverGizmo,
				icon = Resources.Textures.rxUIDetonatorPortable,
				defaultLabel = "PortableDetonator_detonateChannel_label".Translate(Channel),
				defaultDesc = $"{DetonateButtonDesc}\n{GetInspectString()}",
				hotKey = Resources.KeyBinging.rxPortableDetonatorDetonate,
				disabled = !wearer.IsColonist || wearer.Dead || wearer.InMentalState
			};
			if (channelsUpgrade != null && channelsComp != null) {
				if (channelsUpgrade.Complete) {
					var g = channelsComp.GetChannelGizmo();
					if(g != null) yield return g;
				} else {
					var gizmo = channelsUpgrade.CompGetGizmosExtra().FirstOrDefault();
					if (gizmo != null) yield return gizmo;
				}
			}
		}

		private void DrawRangeOverlay() {
			if (!rangeOverlayVisible) return;
			rangeOverlayVisible = false;
			node.DrawRadiusRing();
			node.DrawNetworkLinks();
		}

		private void OnMouseOverGizmo() {
			rangeOverlayVisible = true;
		}

		private void OnGizmoActivation() {
			if (lastActivationTick + ActivationCooldownTicks >= Find.TickManager.TicksGame) return;
			lastActivationTick = Find.TickManager.TicksGame;
			SoundDefOf.FlickSwitch.PlayOneShot(Wearer);

			RemoteTechUtility.TriggerReceiversInNetworkRange(this, channelsComp?.Channel ?? RemoteTechUtility.DefaultChannel);
			
			NumUsesLeft--;
			if (NumUsesLeft <= 0) {
				Destroy(DestroyMode.KillFinalize);
				Messages.Message("PortableDetonator_broke_msg".Translate(), new TargetInfo(Wearer), MessageTypeDefOf.NeutralEvent);
			}
		}

		private void GetCompRefs() {
			channelsUpgrade = this.TryGetUpgrade(ChannelsUpgradeId);
			node = this.RequireComp<CompWirelessDetonationGridNode>();
			channelsComp = GetComp<CompChannelSelector>()?.Configure(false, false, false, RemoteTechUtility.ChannelType.Basic);
		}
	}
}