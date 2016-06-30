using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Linq;

namespace RemoteExplosives
{
	public class Building_DetonatorTable : Building {

		private struct ScheduledTrigger {
			public readonly Building_RemoteExplosive target;
			public readonly int triggerTick;
			public ScheduledTrigger(Building_RemoteExplosive target, int triggerTick) {
				this.target = target;
				this.triggerTick = triggerTick;
			}
		}

		private static readonly Texture2D UITex_Detonate = ContentFinder<Texture2D>.Get("UIDetonate");
		private static readonly string DetonateButtonLabel = "DetonatorTable_detonate_label".Translate();
		private static readonly string DetonateButtonDesc = "DetonatorTable_detonate_desc".Translate();

		private static readonly Texture2D UITex_InstallComponent = ContentFinder<Texture2D>.Get("UIChannelComponent");
		private static readonly string InstallComponentButtonLabel = "DetonatorTable_component_label".Translate();
		private static readonly string InstallComponentButtonDesc = "DetonatorTable_component_desc".Translate();

		private const int FindExplosivesEveryTicks = 120;
		// how long it will take to trigger an additional explosive
		private const int TicksBetweenTriggers = 2;

		private bool wantDetonation;

		private int ticksSinceLastInspection;

		private int numViableExplosives;

		private Pawn lastSeenFloatMenuPawn;

		private RemoteExplosivesUtility.RemoteChannel currentChannel;

		private readonly Queue<ScheduledTrigger> triggerQueue = new Queue<ScheduledTrigger>();

		private bool hasChannelsComponent;
		public bool HasChannelsComponent {
			get { return hasChannelsComponent; }
		}

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

			var armedExplosives = FindArmedExplosivesInRange();
			if(armedExplosives.Count>0) {
				// schedule explosives to be triggered, closer ones first
				armedExplosives = armedExplosives.OrderBy(e => e.Position.DistanceToSquared(Position)).ToList();
				var lastTriggerTime = Find.TickManager.TicksGame+1;
				foreach (var explosive in armedExplosives) {
					triggerQueue.Enqueue(new ScheduledTrigger(explosive, lastTriggerTime));
					lastTriggerTime += TicksBetweenTriggers;
				}
			} else {
				Messages.Message("DetonatorTable_notargets".Translate(), MessageSound.Standard);
			}
		}

		public override void Tick() {
			base.Tick();
			// find explosives in range
			ticksSinceLastInspection++;
			if (ticksSinceLastInspection>=FindExplosivesEveryTicks) {
				ticksSinceLastInspection = 0;
				UpdateNumArmedExplosivesInRange();
			}
			// trigger scheduled explosives
			if(triggerQueue.Count>0) {
				var currentTick = Find.TickManager.TicksGame;
				while (triggerQueue.Count>0) {
					var entry = triggerQueue.Peek();
					if(entry.triggerTick>currentTick) break;
					triggerQueue.Dequeue();
					entry.target.LightFuse();
				}
			}
		}

		private void PlayNeedPowerEffect() {
			var info = SoundInfo.InWorld(this);
			info.volumeFactor = 3f;
			SoundDefOf.PowerOffSmall.PlayOneShot(info);
		}

		private void UpdateNumArmedExplosivesInRange() {
			numViableExplosives = FindArmedExplosivesInRange().Count;
		}

		private List<Building_RemoteExplosive> FindArmedExplosivesInRange() {
			var results = new List<Building_RemoteExplosive>();
			var sample = Find.ListerBuildings.AllBuildingsColonistOfClass<Building_RemoteExplosive>();
			foreach (var explosive in sample) {
				if (explosive.IsArmed && explosive.CurrentChannel == currentChannel && !explosive.FuseLit && TileIsInRange(explosive.Position)) {
					results.Add(explosive);
				}
			}
			return results;
		}

		private bool TileIsInRange(IntVec3 pos) {
			var maxDistance = def.specialDisplayRadius;
			return Mathf.Sqrt(Mathf.Pow(pos.x - Position.x, 2) + Mathf.Pow(pos.z - Position.z, 2)) <= maxDistance;
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
			if (selPawn.Drafted) {
				var entry = new FloatMenuOption {
					action = FloatMenuDetonateNowAction,
					autoTakeable = false,
					label = "DetonatorTable_detonatenow".Translate(),
				};
				if (Find.Reservations.IsReserved(this, Faction.OfColony)) {
					entry.Disabled = true;
					var reservedByName = Find.Reservations.FirstReserverOf(this, Faction.OfColony).Name.ToStringShort;
					entry.label += " " + string.Format("DetonatorTable_detonatenow_reserved".Translate(), reservedByName);
				}
				yield return entry;
			}
			foreach (var option in base.GetFloatMenuOptions(selPawn)) {
				yield return option;
			}
		}

		private void FloatMenuDetonateNowAction() {
			if (lastSeenFloatMenuPawn == null) return;
			if (!wantDetonation) wantDetonation = true;
			var job = new Job(DefDatabase<JobDef>.GetNamed(JobDriver_DetonateRemoteExplosives.JobDefName), this);
			lastSeenFloatMenuPawn.drafter.TakeOrderedJob(job);
		}
	}
}
