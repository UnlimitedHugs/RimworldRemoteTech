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

		private void ApplyGoodwillPenalty(Faction targetFaction, int goodwillLoss) {
			var gasOwnerFaction = Faction.OfPlayer;
			var goodwillBefore = targetFaction.GoodwillWith(gasOwnerFaction);
			// ensure that faction goodwill drops below the usual cap- relevant when gassing large groups
			var caps = FactionGoodwillCaps.GetFromWorld();
			if (!targetFaction.HostileTo(gasOwnerFaction) && gasOwnerFaction == Faction.OfPlayer) {
				caps.OnPlayerBetrayedFaction(targetFaction);
			}
			if (caps.HasPlayerBetrayedFaction(targetFaction) && 
				goodwillBefore - goodwillLoss < FactionGoodwillCaps.DefaultMinNegativeGoodwill) {
				caps.SetMinNegativeGoodwill(targetFaction, goodwillBefore - goodwillLoss);	
			}
			targetFaction.TryAffectGoodwillWith(gasOwnerFaction, -goodwillLoss);
		}
	}
}
