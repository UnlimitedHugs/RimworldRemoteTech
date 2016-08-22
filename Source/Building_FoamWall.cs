using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Linq;

namespace RemoteExplosives {
	// A wall that will kill and trap any pawns and items it is placed on.
	// Contained items are dropped on destruction.
	public class Building_FoamWall : Building {
		private const string TrappedDamageDefName = "FoamWallRekt";
		private bool justCreated;
		private ThingContainer trappedInventory;

		public override void SpawnSetup() {
			base.SpawnSetup();
			if(justCreated) {
				var trappedThings = CrushThingsUnderWall(this);
				if (trappedThings.Count == 0) return;
				if (trappedInventory == null) trappedInventory = new ThingContainer();
				foreach (var trappedThing in trappedThings) {
					trappedInventory.TryAdd(trappedThing);
				}
				justCreated = false;
			}
		}

		public override void PostMake() {
			base.PostMake();
			justCreated = true;
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Deep.LookDeep(ref trappedInventory, "trappedInventory", null);
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
			if(mode == DestroyMode.Kill && trappedInventory!=null) {
				trappedInventory.TryDropAll(Position, ThingPlaceMode.Direct);
			}
			base.Destroy(mode);
		}

		private List<Thing> CrushThingsUnderWall(Thing wall) {
			var thingsList = Find.ThingGrid.ThingsListAt(wall.Position).Where(t => t != wall).ToList();
			var damageDef = DefDatabase<DamageDef>.GetNamed(TrappedDamageDefName);
			foreach (var thing in thingsList) {
				var pawn = thing as Pawn;
				if (pawn != null && !pawn.RaceProps.IsMechanoid && !pawn.Dead) {
					foreach (var activityGroup in pawn.RaceProps.body.GetActivityGroups(PawnCapacityDefOf.Breathing)) {
						var parts = pawn.RaceProps.body.GetParts(PawnCapacityDefOf.Breathing, activityGroup);
						foreach (var bodyPartRecord in parts) {
							pawn.TakeDamage(new DamageInfo(damageDef, 9999, wall, new BodyPartDamageInfo(bodyPartRecord, false)));
						}
					}
					if(pawn.Dead && pawn.IsColonist) {
						Messages.Message(string.Format("FoamWall_death_message".Translate(), pawn.NameStringShort), MessageSound.Negative);
					}
				} else if (thing.def.plant != null) {
					thing.Destroy(DestroyMode.Kill);
				}
			}
			return Find.ThingGrid.ThingsListAt(wall.Position).Where(t => t != wall && (t.def.category == ThingCategory.Item || t.def.category == ThingCategory.Pawn)).ToList();
		}

		public override string GetInspectString() {
			if(trappedInventory!=null) {
				return string.Format("FoamWall_contents".Translate(), trappedInventory.ContentsString);
			}
			return "";
		}
	}
}
