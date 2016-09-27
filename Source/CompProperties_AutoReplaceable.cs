using Verse;

namespace RemoteExplosives {
	public class CompProperties_AutoReplaceable : CompProperties {
		public float forbiddenForSeconds = 30f;

		public CompProperties_AutoReplaceable() {
			compClass = typeof (CompAutoReplaceable);
		}
	}
}