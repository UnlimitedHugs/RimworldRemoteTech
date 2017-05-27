// ReSharper disable UnassignedField.Global
using RimWorld;
using Verse;

namespace RemoteExplosives {
	public class CompProperties_ChemicalExplosive : CompProperties_Explosive {
		public SoundDef breakSound;
		public ThingDef spawnThingDef;
		public int numFoamBlobs;
		public float gasConcentration;
	}
}
