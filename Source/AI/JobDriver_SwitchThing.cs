using System.Collections.Generic;
using RimWorld;
using Verse.AI;

namespace RemoteTech;

/// <summary>
///     Calls a colonist to flick a thing or comp implementing ISwitchable.
/// </summary>
public class JobDriver_SwitchThing : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job);
    }

    public override IEnumerable<Toil> MakeNewToils()
    {
        AddFailCondition(JobHasFailed);
        this.FailOnDestroyedNullOrForbidden(TargetIndex.A);
        yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.Touch);
        yield return new Toil
        {
            initAction = () =>
            {
                TargetThingA.TrySwitch();
                TargetThingA.UpdateSwitchDesignation();
                for (var i = 0; i <= 10; i++)
                {
                    FleckMaker.ThrowDustPuff(TargetThingA.Position, Map, 1);
                }
            },
            defaultCompleteMode = ToilCompleteMode.Instant
        };
    }

    private bool JobHasFailed()
    {
        return !TargetThingA.WantsSwitching();
    }
}