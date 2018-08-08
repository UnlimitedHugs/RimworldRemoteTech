using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using HugsLib.Utils;
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

		private string _effectDescription;
		public string EffectDescription {
			get {
				if (_effectDescription == null) {
					var s = new StringBuilder("Upgrade_descriptionEffects".Translate());
					if (effectDescription != null) {
						s.Append(effectDescription);
					} else {
						for (var i = 0; i < statModifiers.Count; i++) {
							var effect = statModifiers[i];
							s.Append(effect.stat.LabelCap);
							s.Append(": ");
							s.Append(effect.ToStringAsFactor);
							if (i < statModifiers.Count - 1) s.Append(", ");
						}
					}
					_effectDescription = s.ToString();
				}
				return _effectDescription;
			}
		}

		private string _materialsDescription;
		public string MaterialsDescription {
			get {
				if (_materialsDescription == null) {
					var s = new StringBuilder("Upgrade_descriptionCost".Translate());
					for (var i = 0; i < costList.Count; i++) {
						var thingCount = costList[i];
						s.Append(thingCount.count.ToString());
						s.Append("x ");
						s.Append(thingCount.thingDef.label);
						if (i < costList.Count - 1) s.Append(", ");
					}
					_materialsDescription = costList.Count>0 ? s.ToString() : string.Empty;
				}
				return _materialsDescription;
			}
		}

		private string _prerequisitesDescription;
		public string GetPrerequisitesDescription(ThingDef parentDef) {
			if (_prerequisitesDescription == null) {
				var reqList = new List<string>(2);
				if (researchPrerequisite != null) {
					reqList.Add("Upgrade_prerequisitesResearch".Translate(researchPrerequisite.label));
				}
				if (prerequisiteUpgradeId != null) {
					var prereqLabel = parentDef.comps.OfType<CompProperties_Upgrade>().FirstOrDefault(u => u.referenceId == prerequisiteUpgradeId)?.label;
					reqList.Add("Upgrade_prerequisitesUpgrade".Translate(prereqLabel ?? prerequisiteUpgradeId));
				}
				_prerequisitesDescription = reqList.Count > 0 ? "Upgrade_prerequisites".Translate() + reqList.Join(", ") : string.Empty;
			}
			return _prerequisitesDescription;
		}

		public string GetDescriptionPart(ThingDef parentDef) {
			return $"<b>{"Upgrade_labelPrefix".Translate(label)}</b>{MakeSection(EffectDescription)}{MakeSection(MaterialsDescription)}{MakeSection(GetPrerequisitesDescription(parentDef))}";
		}

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

		private string MakeSection(string str) {
			if (str.NullOrEmpty()) return str;
			return "\n" + str;
		}
	}
}