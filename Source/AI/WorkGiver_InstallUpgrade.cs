using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RemoteTech {
	/// <summary>
	/// Provides JobDriver_InstallUpgrade to colonists, based on things that have upgrade designation.
	/// The designation is applied by CompUpgrade using the toggle gizmo.
	/// </summary>
	public class WorkGiver_InstallUpgrade : WorkGiver_Scanner {
		private const int maxIngredientSearchDist = 999;

		public override ThingRequest PotentialWorkThingRequest {
			get { return ThingRequest.ForGroup(ThingRequestGroup.Undefined); }
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
				&& pawn.CanReserveAndReach(t, PathEndMode.InteractionCell, Danger.Deadly);

			if (status && missingIngredient.Count > 0 && TryFindHaulableOfDef(pawn, missingIngredient.ThingDef) == null) {
				JobFailReason.Is("Upgrade_missingMaterials".Translate(missingIngredient.Count, missingIngredient.ThingDef.LabelCap));
				status = false;
			}
			if (status && !comp.PawnMeetsSkillRequirement(pawn)) {
				JobFailReason.Is("ConstructionSkillTooLow".Translate());
				status = false;
			}
			return status ? new Job(Resources.Job.rxInstallUpgrade, t) : null;
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) {
			var candidates = pawn.Map.designationManager.SpawnedDesignationsOfDef(Resources.Designation.rxInstallUpgrade);
			foreach (var des in candidates) {
				var designatedThing = des.target.Thing;
				var comp = designatedThing.FirstUpgradeableComp();
				if (comp != null && comp.WantsWork) {
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