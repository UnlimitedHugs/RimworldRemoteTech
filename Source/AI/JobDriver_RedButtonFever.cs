using System.Collections.Generic;
using Verse.AI;

namespace RemoteExplosives {
	/// <summary>
	/// Drives a pawn with the Red Button Fever mental break to go trigger a detonator.
	/// </summary>
	/// <see cref="JobGiver_RedButtonFever"/>
	/// <see cref="IRedButtonFeverTarget"/>
	public class JobDriver_RedButtonFever : JobDriver {
		public override bool TryMakePreToilReservations(bool errorOnFailed) {
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils() {
			AddFailCondition(() => !(TargetThingA is IRedButtonFeverTarget t) || !t.RedButtonFeverCanInteract);
			var pathEndMode = (TargetThingA?.def?.hasInteractionCell ?? false) ? PathEndMode.InteractionCell : PathEndMode.ClosestTouch;
			this.FailOnDespawnedOrNull(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, pathEndMode);
			yield return new Toil {
				initAction = () => {
					((IRedButtonFeverTarget)TargetThingA).RedButtonFeverDoInteraction(pawn);
					if(pawn.InMentalState) pawn.mindState.mentalStateHandler.CurState.RecoverFromState();
				}
			};
		}
	}
}