using RimWorld;
using Verse;

namespace RemoteExplosives {
	public class BuildingProperties_FoamBlob : BuildingProperties {
		public IntRange TicksToHarden;
		public IntRange TicksBetweenSpreading;
		public ThingDef HardenedDef;
	}
}
