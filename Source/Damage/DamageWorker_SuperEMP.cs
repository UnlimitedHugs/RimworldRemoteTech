using Harmony;
using RimWorld;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Enhanced EMP damage with custom duration and the ability to incapacitate a mechanical pawn at low health
	/// </summary>
	public class DamageWorker_SuperEMP : DamageWorker {

		public override DamageResult Apply(DamageInfo dinfo, Thing victim) {
			var pawn = victim as Pawn;
			// duplicate vanilla emp behavior, since the original def is hardcoded
			if (pawn != null && !pawn.RaceProps.IsFlesh && !pawn.health.Dead && !pawn.health.Downed) {
				var empDef = def as SuperEMPDamageDef ?? new SuperEMPDamageDef();
				if (pawn.stances != null && pawn.stances.stunner != null) {
					pawn.stances.stunner.Notify_DamageApplied(new DamageInfo(DamageDefOf.EMP, dinfo.Amount), true);
				}
				if (pawn.health.summaryHealth.SummaryHealthPercent < empDef.incapHealthThreshold && Rand.Chance(empDef.incapChance)) {
					RemoteExplosivesController.Instance.PawnHealthTrackerMakedDownedMethod.Invoke(pawn.health, new object[]{dinfo, null});
				}
			}
			return base.Apply(dinfo, victim);
		}
	}
}