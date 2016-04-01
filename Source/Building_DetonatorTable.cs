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

		private const int FindExplosivesEveryTicks = 120;
		// how long it will take to trigger an additional explosive
		private const int TicksBetweenTriggers = 2;

		private bool wantDetonation;

		private int ticksSinceLastInspections;

		private int numViableExplosives;

		private Pawn lastSeenFloatMenuPawn;

		private readonly Queue<ScheduledTrigger> triggerQueue = new Queue<ScheduledTrigger>();

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.LookValue(ref wantDetonation, "wantDetonation", false);
		}

		public override IEnumerable<Gizmo> GetGizmos(){
			var detonateGizmo = new Command_Toggle();
			detonateGizmo.toggleAction = DetonateGizmoAction;
			detonateGizmo.isActive = ()=>wantDetonation;
			detonateGizmo.icon = UITex_Detonate;
			detonateGizmo.defaultLabel = DetonateButtonLabel;
			detonateGizmo.defaultDesc = DetonateButtonDesc;
			detonateGizmo.hotKey = KeyBindingDef.Named("RemoteTableDetonate");

			yield return detonateGizmo;
			foreach (var g in base.GetGizmos()) {
				yield return g;
			}
		} 

		private void DetonateGizmoAction() {
			wantDetonation = !wantDetonation;
		}

		public bool WantsDetonation() {
			return wantDetonation;
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

		private void PlayNeedPowerEffect() {
			var info = SoundInfo.InWorld(this);
			info.volumeFactor = 3f;
			SoundDefOf.PowerOffSmall.PlayOneShot(info);
		}

		public override void Tick() {
			base.Tick();
			// find explosives in range
			ticksSinceLastInspections++;
			if (ticksSinceLastInspections>=FindExplosivesEveryTicks) {
				ticksSinceLastInspections = 0;
				numViableExplosives = FindArmedExplosivesInRange().Count;
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

		private List<Building_RemoteExplosive> FindArmedExplosivesInRange() {
			var results = new List<Building_RemoteExplosive>();
			var sample = Find.ListerBuildings.AllBuildingsColonistOfClass<Building_RemoteExplosive>();
			foreach (var explosive in sample) {
				if (explosive.IsArmed && !explosive.FuseLit && TileIsInRange(explosive.Position)) {
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
					entry.label += " " + "DetonatorTable_detonatenow_reserved".Translate().Replace("%", Find.Reservations.FirstReserverOf(this, Faction.OfColony).Name.ToStringShort);
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
