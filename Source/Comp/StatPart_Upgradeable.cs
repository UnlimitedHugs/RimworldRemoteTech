using System.Text;
using RimWorld;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Required for CompUpgrade to modify arbitrary stats.
	/// This is automatically added to StatDefs that have been found to be used in a CompProperties_Upgrade.
	/// </summary>
	/// <see cref="RemoteTechController.InjectUpgradeableStatParts"/>
	public class StatPart_Upgradeable : StatPart {
		public override void TransformValue(StatRequest req, ref float val) {
			if (req.Thing is ThingWithComps tcomps) {
				for (var i = 0; i < tcomps.AllComps.Count; i++) {
					if (tcomps.AllComps[i] is CompUpgrade upgrade && upgrade.Complete) {
						var mod = upgrade.TryGetStatModifier(parentStat);
						if (mod != null) {
							val *= mod.value;
						}
					}
				}
			}
		}

		public override string ExplanationPart(StatRequest req) {
			StringBuilder builder = null;
			if (req.Thing is ThingWithComps tcomps) {
				for (var i = 0; i < tcomps.AllComps.Count; i++) {
					if (tcomps.AllComps[i] is CompUpgrade upgrade && upgrade.Complete) {
						var mod = upgrade.TryGetStatModifier(parentStat);
						if (mod != null) {
							if (builder == null) {
								builder = new StringBuilder("Upgrade_statModifierCategory".Translate());
								builder.AppendLine();
							}
							builder.Append("    ");
							builder.Append(upgrade.Props.label.CapitalizeFirst());
							builder.Append(": ");
							builder.Append(mod.ToStringAsFactor);
						}
					}
				}
			}
			return builder?.ToString();
		}
	}
}