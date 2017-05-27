// ReSharper disable UnassignedField.Global
using Verse;

namespace RemoteExplosives {
	public class CompProperties_WiredDetonationTransmitter : CompProperties {
		public float signalDelayPerTile;

		public CompProperties_WiredDetonationTransmitter() {
			compClass = typeof (CompWiredDetonationTransmitter);
		}
	}
}