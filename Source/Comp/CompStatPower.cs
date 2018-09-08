using RimWorld;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Enables power consumption to be pulled from a stat value, which allows it to be affected by upgrade comps
	/// </summary>
	public class CompStatPower : CompPowerTrader {
		private const int UpdateEveryTicks = 30;

		protected virtual float PowerConsumption {
			get { return parent.GetStatValue(Resources.Stat.rxPowerConsumption); }
		}

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			base.PostSpawnSetup(respawningAfterLoad);
			SetUpPowerVars();
		}

		public override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);
			if (signal == CompUpgrade.UpgradeCompleteSignal) SetUpPowerVars();
		}

		public override void SetUpPowerVars() {
			// allows the comp to switch from consumer to producer
			var prevDefValue = Props.basePowerConsumption;
			Props.basePowerConsumption = PowerConsumption;
			base.SetUpPowerVars();
			Props.basePowerConsumption = prevDefValue;
		}

		public override void CompTick() {
			base.CompTick();
			if (Find.TickManager.TicksGame % UpdateEveryTicks == 0) {
				SetUpPowerVars();
			}
		}
	}
}