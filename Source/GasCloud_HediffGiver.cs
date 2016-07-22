using System;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	public class GasCloud_HediffGiver : GasCloud {

		private MoteProperties_GasCloud_HediffGiver gasProps;

		public override void SpawnSetup() {
			base.SpawnSetup();
			gasProps = (MoteProperties_GasCloud_HediffGiver) def.mote;
			if (gasProps == null) throw new Exception("Missing required gas mote properties in " + def.defName);
		}

		public override void GasTick() {
 			base.GasTick();
			var thingsOnTile = Find.ThingGrid.ThingsListAt(Position);
			for (int i = 0; i < thingsOnTile.Count; i++) {
				var pawn = thingsOnTile[i] as Pawn;
				if (pawn == null || (gasProps.requiresFleshyPawn && !pawn.def.race.IsFlesh) || PawnHasImmunizingApparel(pawn)) continue;
				
				var severityIncrease = gasProps.hediffSeverityPerGastick.RandomInRange * Mathf.Min(1, Concentration / gasProps.FullAlphaConcentration);
				HealthUtility.AdjustSeverity(pawn, gasProps.hediffDef, severityIncrease);
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
