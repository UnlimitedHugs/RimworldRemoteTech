// ReSharper disable UnassignedField.Global
using RimWorld;
using Verse;

namespace RemoteTech {
	public class BuildingProperties_FoamBlob : BuildingProperties {
		public IntRange ticksToHarden;
		public IntRange ticksBetweenSpreading;
		public ThingDef hardenedDef;
	}
}
