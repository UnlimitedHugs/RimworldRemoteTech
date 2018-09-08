using HugsLib;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Stat-based power consumption that will switch to Idle mode when the parent device is not in use.
	/// By default looks for pawns on the interaction cells, but can be called directly to report in-use status.
	/// Requires Normal ticks.
	/// </summary>
	public class CompStatPowerIdle : CompStatPower, IPowerUseNotified {
		private const string IdlePowerUpgradeReferenceId = "IdlePower";
		private const float IdlePowerConsumption = 10f;
		private const float InteractionCellPollIntervalTicks = 20;

		private bool hasUpgrade;

		// saved
		private int _highPowerTicks;

		// we can't override PowerOutput in ComPowerTrader, so we go the sneaky way and use SetUpPowerVars
		private int HighPowerTicksLeft {
			get { return _highPowerTicks; }
			set {
				var wasIdle = _highPowerTicks > 0;
				var isIdle = value > 0;
				_highPowerTicks = Mathf.Max(0, value);
				if (wasIdle != isIdle) {
					SetUpPowerVars();
				}
			}
		}

		protected override float PowerConsumption {
			get { return IdlePowerMode ? IdlePowerConsumption : base.PowerConsumption; }
		}

		private bool IdlePowerMode {
			get { return hasUpgrade && HighPowerTicksLeft == 0; }
		}

		private bool HasIdlePowerUpgrade {
			get { return parent.IsUpgradeCompleted(IdlePowerUpgradeReferenceId); }
		}

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			base.PostSpawnSetup(respawningAfterLoad);
			hasUpgrade = HasIdlePowerUpgrade;
			this.RequireTicker(TickerType.Normal);
		}

		public override void CompTick() {
			if (HighPowerTicksLeft > 0) HighPowerTicksLeft--;
			if (parent.def.hasInteractionCell && GenTicks.TicksGame % InteractionCellPollIntervalTicks == 0) {
				var pawnInCell = parent.InteractionCell.GetFirstPawn(parent.Map);
				if (pawnInCell != null && pawnInCell.IsColonist) {
					ReportPowerUse(3f);
				}
			}
		}

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Values.Look(ref _highPowerTicks, "highPowerTicks");
		}

		public override void ReceiveCompSignal(string signal) {
			hasUpgrade = HasIdlePowerUpgrade;
			base.ReceiveCompSignal(signal);
		}

		public void ReportPowerUse(float duration = 1f) {
			HighPowerTicksLeft = Mathf.Max(HighPowerTicksLeft, duration.SecondsToTicks());
		}
	}
}