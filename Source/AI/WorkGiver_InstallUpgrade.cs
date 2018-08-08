using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	public class WorkGiver_InstallUpgrade : WorkGiver_Scanner {
		private const int maxIngredientSearchDist = 999;

		public override ThingRequest PotentialWorkThingRequest {
			get { return ThingRequest.ForUndefined(); }
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false) {
			var thing = t as ThingWithComps;
			if (thing == null) return null;
			var comp = t.FirstUpgradeableComp();
			if (comp == null) return null;
			var missingIngredient = comp.TryGetNextMissingIngredient();

			var status =
				!pawn.Dead && !pawn.Downed && !pawn.IsBurning()
				&& !comp.parent.Destroyed && !comp.parent.IsBurning()
				&& pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Deadly)
				&& missingIngredient.Count == 0 || TryFindHaulableOfDef(pawn, missingIngredient.ThingDef) != null;

			return status ? new Job(Resources.Job.rxInstallUpgrade, t) : null;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			var candidates = pawn.Map.designationManager.SpawnedDesignationsOfDef(Resources.Designation.rxInstallUpgrade);
			foreach (var des in candidates) {
				var designatedThing = des.target.Thing;
				if ((designatedThing.FirstUpgradeableComp()?.WantsWork).GetValueOrDefault() && pawn.CanReserve(designatedThing)) {
					yield return designatedThing;
				}
			}
		}

		private Thing TryFindHaulableOfDef(Pawn pawn, ThingDef haulableDef) {
			bool SearchPredicate(Thing thing) => !thing.IsForbidden(pawn) && pawn.CanReserve(thing);
			return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(haulableDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), maxIngredientSearchDist, SearchPredicate);
		}
	}
}