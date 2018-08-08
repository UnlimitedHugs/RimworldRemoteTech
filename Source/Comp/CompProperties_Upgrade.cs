using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
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
		public ResearchProjectDef researchPrerequisite;
		public string prerequisiteUpgradeId;

		public CompProperties_Upgrade() {
			compClass = typeof(CompUpgrade);
		}

		public override IEnumerable<string> ConfigErrors(ThingDef parentDef) {
			if (label.NullOrEmpty()) yield return "CompProperties_Upgrade needs a label in def " + parentDef.defName;
			if (statModifiers.NullOrEmpty() && effectDescription.NullOrEmpty()) yield return "CompProperties_Upgrade must have stat effects or effectDescription in def " + parentDef.defName;
			if (referenceId.NullOrEmpty()) yield return "CompProperties_Upgrade needs a referenceId in def " + parentDef.defName;
			Exception ex = null;
			try {
				XmlConvert.VerifyName(referenceId);
			} catch (Exception e) {
				ex = e;
			}
			if(ex != null) yield return $"CompProperties_Upgrade needs a valid referenceId in def {parentDef.defName}: {ex.Message}";
			if (parentDef.comps.OfType<CompProperties_Upgrade>().Count(u => u.referenceId == referenceId) > 1)
				yield return "CompProperties_Upgrade requires a unique referenceId in def " + parentDef.defName;
		}
	}
}