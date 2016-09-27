using Verse;

namespace RemoteExplosives {
	/* 
	 * This hediff will prevent the game from randomly killing off non-colonist pawns when incapacitated by an increase in severity.
	 */
	public class Hediff_NonLethal : HediffWithComps {

		public override float Severity {
			get {
				return base.Severity;
			}
			set {
				var prevValue = pawn.health.forceIncap;
				var customDef = def as HediffDef_NonLethal;
				if(customDef == null || Rand.Range(0f, 1f) >= customDef.vanillaLethalityChance) {
					pawn.health.forceIncap = true;
				}
				base.Severity = value;
				pawn.health.forceIncap = prevValue;
			}
		}
	}
}
