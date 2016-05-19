using RimWorld;
using Verse;

namespace RemoteExplosives {
	public class FoamBlobBuildingProperties : BuildingProperties {
		public IntRange TicksToHarden;
		public IntRange TicksBetweenSpreading;
		public ThingDef HardenedDef;
	}
}
