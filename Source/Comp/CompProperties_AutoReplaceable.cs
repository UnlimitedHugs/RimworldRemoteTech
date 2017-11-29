using Verse;

namespace RemoteExplosives {
	public class CompProperties_AutoReplaceable : CompProperties {
		public bool applyOnVanish;

		public CompProperties_AutoReplaceable() {
			compClass = typeof (CompAutoReplaceable);
		}
	}
}