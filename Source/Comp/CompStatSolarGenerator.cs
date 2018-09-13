using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// A solar plant with stat based power production (rxPowerConsumption) and sun exposure multiplier (rxSunExposure)
	/// </summary>
	public class CompStatSolarGenerator : CompPowerPlantSolar {
		private CachedValue<float> statPowerConsumption;
		private CachedValue<float> statSunExposure;

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

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			statPowerConsumption = parent.GetCachedStat(Resources.Stat.rxPowerConsumption);
			statSunExposure = parent.GetCachedStat(Resources.Stat.rxSunExposure);
		}

		public override void PostExposeData() {
			Scribe.EnterNode(nameof(CompStatSolarGenerator)); // prevents properties collisions from multiple CompPower on the same Thing
			base.PostExposeData();
			Scribe.ExitNode();
		}

		public override void ReceiveCompSignal(string signal) {
			base.ReceiveCompSignal(signal);
			if (signal == CompUpgrade.UpgradeCompleteSignal) {
				statPowerConsumption.Recache();
				statSunExposure.Recache();
			}
		}

		public override void PostDraw() {
			// don't draw the exposure bar
		}
	}
}