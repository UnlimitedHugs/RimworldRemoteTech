using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RemoteExplosives {
	// Finds remote explosive charges in range and detonates them on command.
	// Can be upgraded with a component to unlock the ability to use channels.
	[StaticConstructorOnStartup]
	public class Building_DetonatorTable : Building, IPawnDetonateable {
		private static readonly Texture2D UITex_Detonate = ContentFinder<Texture2D>.Get("UIDetonate");
		private static readonly string DetonateButtonLabel = "DetonatorTable_detonate_label".Translate();
		private static readonly string DetonateButtonDesc = "DetonatorTable_detonate_desc".Translate();

		private static readonly Texture2D UITex_InstallComponent = ContentFinder<Texture2D>.Get("UIChannelComponent");
		private static readonly string InstallComponentButtonLabel = "DetonatorTable_component_label".Translate();
		private static readonly string InstallComponentButtonDesc = "DetonatorTable_component_desc".Translate();

		private const int FindExplosivesEveryTicks = 120;

		private bool wantDetonation;

		private int ticksSinceLastInspection;

		private int numViableExplosives;

		private Pawn lastSeenFloatMenuPawn;

		private RemoteExplosivesUtility.RemoteChannel currentChannel;

		private bool hasChannelsComponent;

		private bool wantChannelsComponent;
		public bool WantChannelsComponent {
			get { return wantChannelsComponent; }
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.LookValue(ref wantDetonation, "wantDetonation", false);
			Scribe_Values.LookValue(ref currentChannel, "currentChannel", RemoteExplosivesUtility.RemoteChannel.White);
			Scribe_Values.LookValue(ref hasChannelsComponent, "hasChannelsComponent", false);
			Scribe_Values.LookValue(ref wantChannelsComponent, "wantChannelsComponent", false);
		}

		public override IEnumerable<Gizmo> GetGizmos(){
			var detonateGizmo = new Command_Toggle {
				toggleAction = DetonateGizmoAction,
				isActive = () => wantDetonation,
				icon = UITex_Detonate,
				defaultLabel = DetonateButtonLabel,
				defaultDesc = DetonateButtonDesc,
				hotKey = KeyBindingDef.Named("RemoteTableDetonate")
			};
			yield return detonateGizmo;

			if (RemoteExplosivesUtility.ChannelsUnlocked()) {
				if (hasChannelsComponent) {
					var channelGizmo = RemoteExplosivesUtility.MakeChannelGizmo(currentChannel, ChannelGizmoAction);
					yield return channelGizmo;
				} else {
					var componentGizmo = new Command_Toggle {
						toggleAction = ComponentGizmoAction,
						isActive = () => wantChannelsComponent,
						icon = UITex_InstallComponent,
						defaultLabel = InstallComponentButtonLabel,
						defaultDesc = InstallComponentButtonDesc
					};
					yield return componentGizmo;
				}
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

		private void ChannelGizmoAction() {
			currentChannel = RemoteExplosivesUtility.GetNextChannel(currentChannel);
			UpdateNumArmedExplosivesInRange();
		}

		public bool UseInteractionCell {
			get { return true; }
		}

		public bool WantsDetonation() {
			return wantDetonation;
		}
		
		public void InstallChannelsComponent() {
			hasChannelsComponent = true;
			wantChannelsComponent = false;
		}

		public void DoDetonation() {
			wantDetonation = false;
			if(!GetComp<CompPowerTrader>().PowerOn) {
				PlayNeedPowerEffect();
				return;
			}
			SoundDefOf.FlickSwitch.PlayOneShot(Position);

			RemoteExplosivesUtility.LightArmedExplosivesInRange(Position, SignalRange, currentChannel);
		}

		public override void Tick() {
			base.Tick();
			// find explosives in range
			ticksSinceLastInspection++;
			if (ticksSinceLastInspection>=FindExplosivesEveryTicks) {
				ticksSinceLastInspection = 0;
				UpdateNumArmedExplosivesInRange();
			}
		}

		private float SignalRange {
			get { return def.specialDisplayRadius; }
		}

		private void PlayNeedPowerEffect() {
			var info = SoundInfo.InWorld(this);
			info.volumeFactor = 3f;
			SoundDefOf.PowerOffSmall.PlayOneShot(info);
		}

		private void UpdateNumArmedExplosivesInRange() {
			numViableExplosives = RemoteExplosivesUtility.FindArmedExplosivesInRange(Position, SignalRange, currentChannel).Count;
		}

		public override string GetInspectString(){
			var stringBuilder = new StringBuilder();
			stringBuilder.Append(base.GetInspectString());
			stringBuilder.AppendLine();
			stringBuilder.Append("DetonatorTable_inrange".Translate());
			stringBuilder.Append(": " + numViableExplosives);
			if(RemoteExplosivesUtility.ChannelsUnlocked()) {
				stringBuilder.AppendLine();
				stringBuilder.Append(RemoteExplosivesUtility.GetCurrentChannelInspectString(currentChannel));
			}
			return stringBuilder.ToString();
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
