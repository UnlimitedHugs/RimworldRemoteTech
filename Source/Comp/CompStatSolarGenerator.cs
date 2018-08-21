using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// A solar plant with stat based power production (rxPowerConsumption) and sun exposure multiplier (rxSunExposure)
	/// </summary>
	public class CompStatSolarGenerator : CompPowerPlantSolar {
		private readonly CachedValue<float> statPowerConsumption;
		private readonly CachedValue<float> statSunExposure;

		protected override float DesiredPowerOutput {
			get {
				var sunExposure = Mathf.Clamp01(parent.Map.skyManager.CurSkyGlow * statSunExposure);
				return Mathf.Lerp(0f, -statPowerConsumption, sunExposure) * RoofedPowerOutputFactor;
			}
		}

		private float RoofedPowerOutputFactor {
			// swiped from CompPowerPlantSolar
			get {
				int totalTiles = 0;
				int tilesCovered = 0;
				foreach (var c in parent.OccupiedRect()) {
					totalTiles++;
					if (parent.Map.roofGrid.Roofed(c)) tilesCovered++;
				}
				return (totalTiles - tilesCovered) / (float)totalTiles;
			}
		}

		public CompStatSolarGenerator() {
			statPowerConsumption = new CachedValue<float>(() => parent.GetStatValue(Resources.Stat.rxPowerConsumption));
			statSunExposure = new CachedValue<float>(() => parent.GetStatValue(Resources.Stat.rxSunExposure));
		}

		public override void Notify_SignalReceived(Signal signal) {
			base.Notify_SignalReceived(signal);
			if (signal.tag == CompUpgrade.UpgradeCompleteSignal) {
				statPowerConsumption.Recache();
				statSunExposure.Recache();
			}
		}

		public override void PostDraw() {
			// don't draw the exposure bar
		}
	}
}