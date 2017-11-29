using System;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/* 
	 * A GasCloud that increases severity of a Hediff on pawns in the same tile.
	 * Supports apparel that will negate this effect.
	 */
	public class GasCloud_HediffGiver : GasCloud {
		private const int IncapGoodwillPenalty = 20; // successful map exit gain is 15
		private const int KillGoodwillPenalty = 50;

		private MoteProperties_GasCloud_HediffGiver gasProps;

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			gasProps = (MoteProperties_GasCloud_HediffGiver) def.mote;
			if (gasProps == null) throw new Exception("Missing required gas mote properties in " + def.defName);
		}

		protected override void GasTick() {
 			base.GasTick();
			if(!Spawned) return;
			var thingsOnTile = Map.thingGrid.ThingsListAt(Position);
			for (int i = 0; i < thingsOnTile.Count; i++) {
				var pawn = thingsOnTile[i] as Pawn;
				if (pawn == null || pawn.Dead || (gasProps.requiresFleshyPawn && !pawn.def.race.IsFlesh) || PawnHasImmunizingApparel(pawn)) continue;
				
				var severityIncrease = gasProps.hediffSeverityPerGastick.RandomInRange * Mathf.Min(1, Concentration / gasProps.FullAlphaConcentration);
				var wasDowned = pawn.Downed;
				HealthUtility.AdjustSeverity(pawn, gasProps.hediffDef, severityIncrease);
				// this should be refactored into applyDamage somehow, but for now this will do
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
			faction.AffectGoodwillWith(Faction.OfPlayer, -goodwillLoss);
		}

		private bool PawnHasImmunizingApparel(Pawn pawn) {
			if(pawn.apparel == null || gasProps.immunizingApparelDefs==null || gasProps.immunizingApparelDefs.Count == 0) return false;
			var apparel = pawn.apparel.WornApparel;
			for (int i = 0; i < apparel.Count; i++) {
				if (gasProps.immunizingApparelDefs.Contains(apparel[i].def)) return true;
			}
			return false;
		}
	}
}
