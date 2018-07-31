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

		private static readonly string InstallComponentButtonLabel = "DetonatorTable_component_label".Translate();
		private static readonly string InstallComponentButtonDesc = "DetonatorTable_component_desc".Translate();

		private const int FindExplosivesEveryTicks = 30;

		private bool wantDetonation;

		private int lastInspectionTick;

		private Dictionary<int, List<Building_RemoteExplosive>> explosivesInRange;

		private int currentChannel = 1;

		private bool hasChannelsComponent;

		private bool wantChannelsComponent;

		public bool WantChannelsComponent {
			get { return wantChannelsComponent; }
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Values.Look(ref wantDetonation, "wantDetonation");
			Scribe_Values.Look(ref currentChannel, "currentChannel");
			Scribe_Values.Look(ref hasChannelsComponent, "hasChannelsComponent");
			Scribe_Values.Look(ref wantChannelsComponent, "wantChannelsComponent");
		}

		public override IEnumerable<Gizmo> GetGizmos() {
			var detonateGizmo = new Command_Toggle {
				toggleAction = DetonateGizmoAction,
				isActive = () => wantDetonation,
				icon = Resources.Textures.UIDetonate,
				defaultLabel = DetonateButtonLabel,
				defaultDesc = DetonateButtonDesc,
				hotKey = Resources.KeyBinging.RemoteTableDetonate
			};
			yield return detonateGizmo;

			if (hasChannelsComponent) {
				var channelGizmo = RemoteExplosivesUtility.GetChannelGizmo(currentChannel, currentChannel, ChannelGizmoAction, RemoteExplosivesUtility.GetChannelsUnlockLevel(), explosivesInRange);
				if (channelGizmo != null) {
					yield return channelGizmo;
				}
			} else if (RemoteExplosivesUtility.GetChannelsUnlockLevel() > RemoteExplosivesUtility.ChannelType.None) {
				var componentGizmo = new Command_Toggle {
					toggleAction = ComponentGizmoAction,
					isActive = () => wantChannelsComponent,
					icon = Resources.Textures.UIChannelComponent,
					defaultLabel = InstallComponentButtonLabel,
					defaultDesc = InstallComponentButtonDesc
				};
				yield return componentGizmo;
			}

			foreach (var g in base.GetGizmos()) {
				yield return g;
			}
		}

		private void ComponentGizmoAction() {
			wantChannelsComponent = !wantChannelsComponent;
		}

		private void DetonateGizmoAction() {
			wantDetonation = !wantDetonation;
		}

		private void ChannelGizmoAction(int selectedChannel) {
			currentChannel = selectedChannel;
			UpdateArmedExplosivesInRange();
		}

		public bool UseInteractionCell {
			get { return true; }
		}

		public bool WantsDetonation {
			get { return wantDetonation; }
			set { wantDetonation = value; }
		}

		public void InstallChannelsComponent() {
			hasChannelsComponent = true;
			wantChannelsComponent = false;
		}

		public void DoDetonation() {
			wantDetonation = false;
			if (!GetComp<CompPowerTrader>().PowerOn) {
				PlayNeedPowerEffect();
				return;
			}
			SoundDefOf.FlickSwitch.PlayOneShot(this);
			RemoteExplosivesUtility.LightArmedExplosivesInRange(Position, Map, SignalRange, currentChannel);
		}

		private float SignalRange {
			get { return def.specialDisplayRadius; }
		}

		private void PlayNeedPowerEffect() {
			var info = SoundInfo.InMap(this);
			info.volumeFactor = 3f;
			SoundDefOf.Power_OffSmall.PlayOneShot(info);
		}

		private void UpdateArmedExplosivesInRange() {
			lastInspectionTick = GenTicks.TicksGame;
			explosivesInRange = RemoteExplosivesUtility.FindArmedExplosivesInRange(Position, Map, SignalRange);
		}

		public override string GetInspectString() {
			// update channel contents
			if (lastInspectionTick + FindExplosivesEveryTicks < GenTicks.TicksGame) {
				UpdateArmedExplosivesInRange();
			}
			// assemble info string
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(base.GetInspectString());
			if (explosivesInRange != null) {
				explosivesInRange.TryGetValue(currentChannel, out List<Building_RemoteExplosive> list);
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
