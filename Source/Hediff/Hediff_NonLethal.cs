using Verse;

namespace RemoteTech;

/// <summary>
///     This hediff will prevent the game from randomly killing off non-colonist pawns when incapacitated by an increase in
///     severity.
/// </summary>
public class Hediff_NonLethal : HediffWithComps
{
    public override float Severity
    {
        get => base.Severity;
        set
        {
            var prevValue = pawn.health.forceDowned;
            var customDef = def as HediffDef_NonLethal;
            if (customDef == null || Rand.Range(0f, 1f) >= customDef.vanillaLethalityChance)
            {
                pawn.health.forceDowned = true;
            }

            base.Severity = value;
            pawn.health.forceDowned = prevValue;
        }
    }
}