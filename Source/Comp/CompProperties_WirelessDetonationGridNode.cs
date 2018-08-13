using Verse;

namespace RemoteExplosives {
	public class CompProperties_WirelessDetonationGridNode : CompProperties {
		// endpoints don't connect to each other and don't act as retransmitters
		public bool endpoint;
		public CompProperties_WirelessDetonationGridNode() {
			compClass = typeof(CompWirelessDetonationGridNode);
		}
	}
}