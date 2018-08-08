using System.Collections.Generic;
using RimWorld;
using Verse;
// ReSharper disable CollectionNeverUpdated.Global

namespace RemoteExplosives {
	public class CompProperties_Upgrade : CompProperties {
		public string label;
		public string referenceId;
		public string effectDescription;
		public List<StatModifier> statModifiers = new List<StatModifier>(0);
		public List<ThingDefCountClass> costList = new List<ThingDefCountClass>(0);
		public int workAmount = 1000;

		public CompProperties_Upgrade() {
			compClass = typeof(CompUpgrade);
		}

		public override IEnumerable<string> ConfigErrors(ThingDef parentDef) {
			if (label.NullOrEmpty()) yield return "CompProperties_Upgrade needs a label: " + parentDef.defName;
			if (statModifiers.NullOrEmpty() && effectDescription.NullOrEmpty()) yield return "CompProperties_Upgrade must have stat effects or effectDescription: " + parentDef.defName;
		}
	}
}