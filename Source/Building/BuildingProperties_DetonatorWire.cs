// ReSharper disable UnassignedField.Global
using RimWorld;
using Verse;

namespace RemoteExplosives {
	public class BuildingProperties_DetonatorWire : BuildingProperties {
		public float failureChanceWhenFullyWet = 0.05f;
		public float daysToSelfDry = .8f;
		public float baseDryingTemperature = 20f;
		public EffecterDef failureEffecter;
		public bool fireOnFailure = true;
		public int dryOffJobDurationTicks = 60;
	}
}