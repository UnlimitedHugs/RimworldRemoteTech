using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	/*
	 * Finds remote explosive charges in range and detonates them on command.
	 * Can be upgraded with a component to unlock the ability to use channels.
	 */
	public class Building_DetonatorTable : Building, IPawnDetonateable {
		private static readonly string DetonateButtonLabel = "DetonatorTable_detonate_label".Translate();
		private static readonly string DetonateButtonDesc = "DetonatorTable_detonate_desc".Translate();

		private const string ChannelsBasicUpgradeId = "ChannelsBasic";
		private const string ChannelsAdvancedUpgradeId = "ChannelsAdvanced";
		private const int FindExplosivesEveryTicks = 30;

		private bool wantDetonation;
		private int lastInspectionTick;
		private Dictionary<int, List<IWirelessDetonationReceiver>> explosivesInRange;
		private int currentChannel = 1;
		private CompUpgrade channelsBasic;
		private CompUpgrade channelsAdvanced;

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref wantDetonation, "wantDetonation");
			Scribe_Values.Look(ref currentChannel, "currentChannel", 1);
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			channelsBasic = this.TryGetUpgrade(ChannelsBasicUpgradeId);
			channelsAdvanced = this.TryGetUpgrade(ChannelsAdvancedUpgradeId);
		}

		public override IEnumerable<Gizmo> GetGizmos() {
			var detonateGizmo = new Command_Toggle {
				toggleAction = DetonateGizmoAction,
				isActive = () => wantDetonation,
				icon = Resources.Textures.UIDetonate,
				defaultLabel = DetonateButtonLabel,
				defaultDesc = DetonateButtonDesc,
				hotKey = Resources.KeyBinging.rxRemoteTableDetonate
			};
			yield return detonateGizmo;

			var channelsLevel = RemoteExplosivesUtility.ChannelType.None;
			if (channelsAdvanced != null && channelsAdvanced.Complete) {
				channelsLevel = RemoteExplosivesUtility.ChannelType.Advanced;
			} else if (channelsBasic != null && channelsBasic.Complete) {
				channelsLevel = RemoteExplosivesUtility.ChannelType.Basic;
			}
			if (channelsLevel != RemoteExplosivesUtility.ChannelType.None) {
				var channelGizmo = RemoteExplosivesUtility.GetChannelGizmo(currentChannel, currentChannel, ChannelGizmoAction, channelsLevel, explosivesInRange);
				if (channelGizmo != null) {
					yield return channelGizmo;
				}
			}
			foreach (var g in base.GetGizmos()) {
				yield return g;
			}
		}

		private void DetonateGizmoAction() {
			wantDetonation = !wantDetonation;
		}

		private void ChannelGizmoAction(int selectedChannel) {
			currentChannel = selectedChannel;
			UpdateArmedExplosivesInRange();
			RemoteExplosivesUtility.ReportPowerUse(this, 2f);
		}

		public bool UseInteractionCell {
			get { return true; }
		}

		public bool WantsDetonation {
			get { return wantDetonation; }
			set { wantDetonation = value; }
		}

		public void DoDetonation() {
			wantDetonation = false;
			if (!GetComp<CompPowerTrader>().PowerOn) {
				PlayNeedPowerEffect();
				return;
			}
			RemoteExplosivesUtility.ReportPowerUse(this, 20f);
			SoundDefOf.FlickSwitch.PlayOneShot(this);
			RemoteExplosivesUtility.LightArmedExplosivesInNetworkRange(this, currentChannel);
		}

		private void PlayNeedPowerEffect() {
			var info = SoundInfo.InMap(this);
			info.volumeFactor = 3f;
			SoundDefOf.Power_OffSmall.PlayOneShot(info);
		}

		private void UpdateArmedExplosivesInRange() {
			lastInspectionTick = GenTicks.TicksGame;
			explosivesInRange = RemoteExplosivesUtility.FindArmedExplosivesInNetworkRange(this);
		}

		public override string GetInspectString() {
			if (!Spawned) return string.Empty;
			// update channel contents
			if (lastInspectionTick + FindExplosivesEveryTicks < GenTicks.TicksGame) {
				UpdateArmedExplosivesInRange();
			}
			// assemble info string
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(base.GetInspectString());
			if (explosivesInRange != null) {
				explosivesInRange.TryGetValue(currentChannel, out List<IWirelessDetonationReceiver> list);
				stringBuilder.AppendLine();
				stringBuilder.Append("DetonatorTable_inrange".Translate());
				stringBuilder.Append(": " + (list?.Count).GetValueOrDefault());
			}
			if (RemoteExplosivesUtility.GetChannelsUnlockLevel() > RemoteExplosivesUtility.ChannelType.None) {
				stringBuilder.AppendLine();
				stringBuilder.Append(RemoteExplosivesUtility.GetCurrentChannelInspectString(currentChannel));
			}
			return stringBuilder.ToString();
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
