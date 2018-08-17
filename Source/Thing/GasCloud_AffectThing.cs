using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// To be used as a base for custom gases that affect things in the cell they are located in.
	/// Effect strength is based on gas concentration.
	/// Pawn effects scale based on the ToxicSensitivity stat, and MoteProperties_GasEffect 
	/// can specify apparel that will negate the effects.
	/// </summary>
	public abstract class GasCloud_AffectThing : GasCloud {
		protected MoteProperties_GasEffect Props;

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			if((Props = def.mote as MoteProperties_GasEffect) == null) RemoteExplosivesController.Instance.Logger.Error($"{nameof(GasCloud_AffectThing)} needs {nameof(MoteProperties_GasEffect)} in def " + def.defName); 
		}

		protected override void GasTick() {
			base.GasTick();
			if (!Spawned || gasTicksProcessed % Props.effectInterval != 0) return;
			var thingsOnTile = Map.thingGrid.ThingsListAt(Position);
			for (int i = 0; i < thingsOnTile.Count; i++) {
				var t = thingsOnTile[i];
				if(t == this) continue;
				var multiplier = 0f;
				if (t is Pawn pawn && !pawn.Dead && (Props.affectsDownedPawns || !pawn.Downed) 
					&& (Props.affectsFleshy && pawn.def.race.IsFlesh || Props.affectsMechanical && pawn.RaceProps.IsMechanoid)) {
					multiplier = 1 * GetImmunizingApparelMultiplier(pawn) * GetSensitivityStatMultiplier(pawn);
				} else if (Props.affectsPlants && t is Plant || Props.affectsThings) {
					multiplier = 1f;
				}
				multiplier *= Mathf.Clamp01(Concentration / Props.FullAlphaConcentration);
				multiplier = Mathf.Clamp01(multiplier);
				if (multiplier > 0f) {
					ApplyGasEffect(t, multiplier);
				}
			}
		}

		protected abstract void ApplyGasEffect(Thing thing, float strengthMultiplier);

		protected float GetSensitivityStatMultiplier(Pawn pawn) {
			if (Props.toxicSensitivityStatPower > 0f) {
				return 1f - (1f - Mathf.Clamp01(pawn.GetStatValue(StatDefOf.ToxicSensitivity))) * Props.toxicSensitivityStatPower;
			}
			return 1f;
		}

		protected float GetImmunizingApparelMultiplier(Pawn pawn) {
			if (Props.immunizingApparelPower != 0 && pawn.apparel != null) {
				var apparel = pawn.apparel.WornApparel;
				for (int i = 0; i < apparel.Count; i++) {
					if (Props.immunizingApparelDefs.Contains(apparel[i].def))
						return 1f - Props.immunizingApparelPower;
				}
			}
			return 1f;
		}
	}
}