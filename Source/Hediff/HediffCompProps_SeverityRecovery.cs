// ReSharper disable UnassignedField.Global
using Verse;

namespace RemoteTech {
	public class HediffCompProps_SeverityRecovery : HediffCompProperties {
		public FloatRange severityRecoveryPerTick;
		public int cooldownAfterSeverityIncrease;
		public float severityIncreaseDetectionThreshold;
	}
}
