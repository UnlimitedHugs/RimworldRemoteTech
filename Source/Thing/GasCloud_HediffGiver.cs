using HugsLib.Utils;
using RimWorld;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// A GasCloud that increases severity of a Hediff on pawns in the same tile.
	/// See MoteProperties_GasEffect for setting.
	/// </summary>
	public class GasCloud_HediffGiver : GasCloud_AffectThing {
		private const int IncapGoodwillPenalty = 20; // successful map exit gain is 15
		private const int KillGoodwillPenalty = 50;
		
		protected override void ApplyGasEffect(Thing thing, float strengthMultiplier) {
			if (thing is Pawn pawn) {
				var severityIncrease = Props.hediffSeverityPerGastick.RandomInRange * strengthMultiplier;
				var wasDowned = pawn.Downed;
				HealthUtility.AdjustSeverity(pawn, Props.hediffDef, severityIncrease);
				if (pawn.Faction != null && pawn.Faction != Faction.OfPlayer) {
					if (pawn.Dead) {
						ApplyGoodwillPenalty(pawn.Faction, KillGoodwillPenalty);
					} else if (!wasDowned && pawn.Downed) {
						ApplyGoodwillPenalty(pawn.Faction, IncapGoodwillPenalty);
					}
				}
			}
		}

		private void ApplyGoodwillPenalty(Faction faction, int goodwillLoss) {
			var goodwillBefore = faction.PlayerGoodwill;
			// ensure that faction goodwill drops below the usual cap- relevant when gassing large groups
			if (goodwillBefore - goodwillLoss < CustomFactionGoodwillCaps.DefaultMinNegativeGoodwill) {
				UtilityWorldObjectManager.GetUtilityWorldObject<CustomFactionGoodwillCaps>().SetMinNegativeGoodwill(faction, goodwillBefore - goodwillLoss);	
			}
			faction.TryAffectGoodwillWith(Faction.OfPlayer, -goodwillLoss);
		}
	}
}
