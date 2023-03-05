using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace RemoteTech;

/// <summary>
///     Calls a colonist to a marked detonation wire to dry it off
/// </summary>
public class JobDriver_DryDetonatorWire : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        AddFailCondition(JobHasFailed);
        var wire = TargetThingA as Building_DetonatorWire;
        if (wire == null)
        {
            yield break;
        }

        yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
        var jobDuration = wire.DryOffJobDuration;
        yield return Toils_General.Wait(jobDuration).WithEffect(EffecterDefOf.Clean, TargetIndex.A)
            .WithProgressBarToilDelay(TargetIndex.A, jobDuration);
        yield return new Toil
        {
            initAction = () =>
            {
                if (wire.WantDrying)
                {
                    wire.DryOff();
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
    }

    private bool JobHasFailed()
    {
        var wire = TargetThingA as Building_DetonatorWire;
        return TargetThingA == null || TargetThingA.Destroyed || wire == null || !wire.WantDrying;
    }
}