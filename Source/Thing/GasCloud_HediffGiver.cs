using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/* 
	 * A GasCloud that increases severity of a Hediff on pawns in the same tile.
	 * Supports apparel that will negate this effect.
	 */
	public class GasCloud_HediffGiver : GasCloud {
		private const int IncapGoodwillPenalty = 20;
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
						pawn.Faction.AffectGoodwillWith(Faction.OfPlayer, -KillGoodwillPenalty);
					} else if (!wasDowned && pawn.Downed) {
						pawn.Faction.AffectGoodwillWith(Faction.OfPlayer, -IncapGoodwillPenalty);	
					}
				}
			}
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
