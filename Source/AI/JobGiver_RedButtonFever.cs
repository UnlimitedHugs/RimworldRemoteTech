using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	/// <summary>
	/// For pawns with the Red button fever mental break. Gives them a job to interact with a detonator.
	/// </summary>
	/// <see cref="IRedButtonFeverTarget"/>
	public class JobGiver_RedButtonFever : ThinkNode_JobGiver {
		private IntRange waitTicks = new IntRange(80, 140);

		protected override Job TryGiveJob(Pawn pawn) {
			var targets = pawn.Map.listerBuildings.allBuildingsColonist.Where(b => (b is IRedButtonFeverTarget i && i.RedButtonFeverCanInteract))
				.Select(b => new Pair<Building, float>(b, b.Position.DistanceTo(pawn.Position))).ToArray();
			if (targets.Length > 0) {
				var furthestDistance = targets.Max(p => p.Second);
				// prefer closer ones, but leave room for chance
				var target = targets.RandomElementByWeight(p => furthestDistance - (p.Second / 2f)).First as IRedButtonFeverTarget;
				var targetThing = target as Thing;
				var pathEndMode = (targetThing?.def?.hasInteractionCell ?? false) ? PathEndMode.InteractionCell : PathEndMode.ClosestTouch;
				if (targetThing != null && pawn.CanReach(targetThing, pathEndMode, Danger.Deadly)) {
					return new Job(Resources.Job.rxRedButtonFever, targetThing);
				}
			}

			if (pawn.mindState.nextMoveOrderIsWait) {
				var job = new Job(JobDefOf.Wait_Wander) { expiryInterval = waitTicks.RandomInRange };
				pawn.mindState.nextMoveOrderIsWait = false;
				return job;
			}
			var c = RCellFinder.RandomWanderDestFor(pawn, pawn.Position, 10f, null, Danger.Deadly);
			if (c.IsValid) {
				pawn.mindState.nextMoveOrderIsWait = true;
				return new Job(JobDefOf.GotoWander, c);
			}
			return null;
		}

		public override ThinkNode DeepCopy(bool resolve = true) {
			var giver = (JobGiver_RedButtonFever)base.DeepCopy(resolve);
			giver.waitTicks = waitTicks;
			return giver;
		}
	}
}