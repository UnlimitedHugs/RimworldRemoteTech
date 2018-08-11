using RimWorld;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Enables power consumption to be pulled from a stat value, which allows it to be affected by upgrade comps
	/// </summary>
	public class CompStatPower : CompPowerTrader {
		private const int UpdateEveryTicks = 30;

		private float StatPowerConsumption {
			get { return parent.GetStatValue(Resources.Stat.rxPowerConsumption); }
		}

		public override void SetUpPowerVars() {
			var prevDefValue = StatPowerConsumption; // allows the comp to switch from consumer to producer
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