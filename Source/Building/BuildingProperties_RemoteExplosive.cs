// ReSharper disable UnassignedField.Global
using RimWorld;
using UnityEngine;

namespace RemoteExplosives {
	public class BuildingProperties_RemoteExplosive : BuildingProperties {
		public bool startsArmed = false;
		public Vector3 blinkerOffset;
		public int blinkerIntervalArmed = 100;
		public int blinkerIntervalLit = 7;
	}
}
