// ReSharper disable UnassignedField.Global
using RimWorld;
using Verse;

namespace RemoteExplosives {
	public class BuildingProperties_FoamBlob : BuildingProperties {
		public IntRange ticksToHarden;
		public IntRange ticksBetweenSpreading;
		public ThingDef hardenedDef;
	}
}
