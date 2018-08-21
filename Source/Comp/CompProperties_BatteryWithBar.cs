using RimWorld;
using UnityEngine;

namespace RemoteExplosives {
	public class CompProperties_BatteryWithBar : CompProperties_Battery {
		public float passiveDischargeWatts = 0f;
		public Vector3 barOffset = new Vector3(0f, .1f, 0f);
		public Vector2 barSize = new Vector2(1.3f, 0.4f);
		public float barMargin = 0.15f;

		public CompProperties_BatteryWithBar() {
			compClass = typeof(CompStatBattery);
		}
	}
}