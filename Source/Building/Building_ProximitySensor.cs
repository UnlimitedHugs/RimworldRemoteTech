using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	public partial class Building_ProximitySensor : Building, ISwitchable, ISensorSettingsProvider {
		private const string WirelessUpgrageReferenceId = "WirelessDetonation";
		private const string AIUpgrageReferenceId = "AIController";

		private readonly List<IntVec3> drawnCells = new List<IntVec3>();
		private readonly List<Pawn> trackedPawns = new List<Pawn>();
		private bool isSelected;
		private RadialGradientArea area;
		private CachedValue<float> angleStat;
		private CachedValue<float> speedStat;
		private CachedValue<float> rangeStat;
		private CompPowerTrader powerComp;
		private CompWiredDetonationSender wiredComp;
		private CompWirelessDetonationGridNode wirelessComp;
		private CompUpgrade brainComp;
		private CompChannelSelector channelsComp;

		// saved
		private Arc slice;
		public float lastTriggeredTick;
		private SensorSettings settings = new SensorSettings();
		private SensorSettings pendingSettings;

		private float CooldownTime {
			get { return Mathf.Max(0, lastTriggeredTick + settings.CooldownTime.SecondsToTicks() - GenTicks.TicksGame) / GenTicks.TicksPerRealSecond; }
		}

		public SensorSettings Settings {
			get { return pendingSettings ?? settings; }
		}
		public bool HasWirelessUpgrade {
			get { return wirelessComp != null && wirelessComp.Enabled; }
		}
		public bool HasAIUpgrade {
			get { return brainComp != null && brainComp.Complete; }
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			angleStat = this.GetCachedStat(Resources.Stat.rxSensorAngle);
			speedStat = this.GetCachedStat(Resources.Stat.rxSensorSpeed);
			rangeStat = this.GetCachedStat(Resources.Stat.rxSensorRange);
			powerComp = GetComp<CompPowerTrader>();
			wiredComp = GetComp<CompWiredDetonationSender>();
			wirelessComp = GetComp<CompWirelessDetonationGridNode>();
			channelsComp = GetComp<CompChannelSelector>();
			brainComp = this.TryGetUpgrade(AIUpgrageReferenceId);
			UpdateUpgradeableStuff();
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Deep.Look(ref slice, "slice");
			Scribe_Values.Look(ref lastTriggeredTick, "lastTriggered");
			Scribe_Deep.Look(ref settings, "settings");
			Scribe_Deep.Look(ref pendingSettings, "pendingSettings");
			if(Scribe.mode == LoadSaveMode.LoadingVars && settings == null) settings = new SensorSettings();
		}

		public override void Tick() {
			if (GenTicks.TicksGame % 6 != 0 || (powerComp != null && !powerComp.PowerOn)) return;
			slice = slice.Rotate(speedStat / GenTicks.TicksPerRealSecond);
			drawnCells.Clear();
			var thingGrid = Map.thingGrid;
			// visit all cells in slice
			foreach (var cell in area.CellsInSlice(slice)) {
				if (!cell.InBounds(Map)) continue;
				Pawn pawn = null;
				// find first pawn in cell
				var cellThings = thingGrid.ThingsListAtFast(cell);
				for (var i = 0; i < cellThings.Count; i++) {
					if (cellThings[i] is Pawn p) {
						pawn = p;
						break;
					}
				}
				// track pawn and report them to the authorities
				if (pawn != null && !trackedPawns.Contains(pawn) && GenSight.LineOfSight(Position, cell, Map, true)
					&& CooldownTime == 0f && PawnMatchesFilter(pawn)) {
					TriggerSensor(pawn);
				}
				// store cell position for drawing
				if (isSelected && GenSight.LineOfSight(Position, cell, Map, true)) {
					drawnCells.Add(cell);
				}
			}
			// prune tracked pawns that have left the area
			for (int i = trackedPawns.Count - 1; i >= 0; i--) {
				if (Position.DistanceTo(trackedPawns[i].Position) > rangeStat) trackedPawns.RemoveAt(i);
			}
			isSelected = false;
		}

		public void TriggerSensor(Pawn pawn) {
			lastTriggeredTick = GenTicks.TicksGame;
			trackedPawns.Add(pawn);
			if(settings.SendMessage) NotifyPlayer(pawn);
			if(settings.SendWired && wiredComp != null) wiredComp.SendNewSignal();
			if(settings.SendWireless && wirelessComp != null && wirelessComp.Enabled && channelsComp != null)
				RemoteExplosivesUtility.TriggerReceiversInNetworkRange(this, channelsComp.Channel);
		}

		public override void DrawExtraSelectionOverlays() {
			if (powerComp == null || !powerComp.PowerOn) return;
			base.DrawExtraSelectionOverlays();
			isSelected = true;
			GenDraw.DrawFieldEdges(drawnCells);
			for (int i = 0; i < trackedPawns.Count; i++) {
				GenDraw.DrawCooldownCircle(trackedPawns[i].DrawPos - Altitudes.AltIncVect, .5f);
			}
		}

		public override IEnumerable<Gizmo> GetGizmos() {
			yield return new Command_Action {
				defaultLabel = "proxSensor_settings".Translate(),
				icon = Resources.Textures.UISensorSettings,
				action = OpenSettingsDialog
			};
			foreach (var gizmo in base.GetGizmos()) {
				yield return gizmo;
			}
		}

		public bool WantsSwitch() {
			return !settings.Equals(pendingSettings);
		}

		public void DoSwitch() {
			settings = pendingSettings?.Clone() ?? new SensorSettings();
		}

		public override string GetInspectString() {
			var s = new StringBuilder(base.GetInspectString());
			if (CooldownTime > 0f) {
				s.AppendLine();
				s.AppendFormat("proxSensor_cooldown".Translate(), CooldownTime);
			}
			if (brainComp != null && brainComp.Complete) {
				s.AppendLine();
				s.AppendFormat("proxSensor_AIStatus".Translate());
			}
			return s.ToString();
		}

		protected override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);
			if (signal == CompUpgrade.UpgradeCompleteSignal) {
				UpdateUpgradeableStuff();
				HitPoints = MaxHitPoints; // AI upgrade increases max HP
			}
		}

		private void OnSettingsChanged(SensorSettings s) {
			pendingSettings = s;
			this.UpdateSwitchDesignation();
		}

		private void OpenSettingsDialog() {
			Find.WindowStack.Add(new Dialog_SensorSettings(this) {
				OnSettingsChanged = OnSettingsChanged
			});
		}

		private bool PawnMatchesFilter(Pawn p) {
			if (brainComp == null || !brainComp.Complete || p.RaceProps == null) return true;
			return (settings.DetectAnimals && p.RaceProps.Animal)
					|| (settings.DetectEnemies && p.HostileTo(Faction))
					|| (settings.DetectFriendlies && (p.Faction == null || !p.Faction.HostileTo(Faction)));
		}

		private void NotifyPlayer(Pawn pawn) {
			var message = settings.Name.NullOrEmpty() ? "proxSensor_message".Translate(pawn.LabelShort) : "proxSensor_messageName".Translate(settings.Name, pawn.LabelShort);
			Messages.Message(message, pawn, MessageTypeDefOf.CautionInput);
		}

		private void UpdateUpgradeableStuff() {
			area = new RadialGradientArea(Position, rangeStat.ValueRecached);
			slice = new Arc(slice.StartAngle, angleStat.ValueRecached);
			speedStat.Recache();
			if (wirelessComp != null) wirelessComp.Enabled = this.IsUpgradeCompleted(WirelessUpgrageReferenceId);
			channelsComp?.Configure(true, true, true, this.IsUpgradeCompleted(WirelessUpgrageReferenceId) ? RemoteExplosivesUtility.ChannelType.Advanced : RemoteExplosivesUtility.ChannelType.None);
		}

		#region support stuff

		private struct Arc : IExposable {
			public float StartAngle;
			public float Width;

			public Arc(float startAngle, float width) {
				StartAngle = Mathf.Repeat(startAngle, 360f);
				Width = Mathf.Min(360f, width);
			}

			public Arc Rotate(float degrees) {
				return new Arc(StartAngle + degrees, Width);
			}

			public void ExposeData() {
				Scribe_Values.Look(ref StartAngle, "start");
				Scribe_Values.Look(ref Width, "width");
			}
		}

		/// <summary>
		/// A performance-friendly way to query a circular area of map cells in a given arc from the starting position.
		/// Cell angles are pre-calculated, allowing for O(1) time queries.
		/// </summary>
		private class RadialGradientArea {
			public struct Enumerable {
				private readonly CellAngle[] cells;
				private readonly Arc arc;

				public Enumerable(CellAngle[] cells, Arc arc) {
					this.cells = cells;
					this.arc = arc;
				}

				public Enumerator GetEnumerator() {
					return new Enumerator(cells, AngleToIndex(arc.StartAngle), AngleToIndex(arc.StartAngle + arc.Width));
				}

				private int AngleToIndex(float angle) {
					return Mathf.FloorToInt((angle / 360f) * cells.Length);
				}
			}

			public struct Enumerator {
				private readonly CellAngle[] cells;
				private readonly int endIndex;
				private int index;

				public Enumerator(CellAngle[] cells, int startIndex, int endIndex) {
					this.cells = cells;
					this.endIndex = endIndex;
					index = startIndex - 1;
				}

				public IntVec3 Current {
					get { return cells[index % cells.Length].Cell; }
				}

				public bool MoveNext() {
					return ++index <= endIndex;
				}
			}

			public struct CellAngle {
				public readonly IntVec3 Cell;
				public readonly float Angle;

				public CellAngle(IntVec3 cell, float angle) {
					Cell = cell;
					Angle = angle;
				}
			}

			private readonly CellAngle[] cells;

			public RadialGradientArea(IntVec3 center, float radius) {
				cells = GenRadial.RadialCellsAround(center, radius, false)
					.Select(c => new CellAngle(c, (c - center).AngleFlat))
					.OrderBy(c => c.Angle)
					.ToArray();
			}

			public Enumerable CellsInSlice(Arc arc) {
				return new Enumerable(cells, arc);
			}
		}
	}
	#endregion
}