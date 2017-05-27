using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RemoteExplosives {
	/* 
	 * A wall that will kill and trap any pawns and items it is placed on.
	 * Contained items are dropped on destruction.
	 */
	public class Building_FoamWall : Building {
		private bool justCreated;
		private ThingOwner<Thing> trappedInventory;

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			if(justCreated) {
				var trappedThings = CrushThingsUnderWall(this);
				if (trappedThings.Count == 0) return;
				if (trappedInventory == null) trappedInventory = new ThingOwner<Thing>();
				foreach (var trappedThing in trappedThings) {
					if (trappedThing.holdingOwner != null) {
						trappedThing.holdingOwner.TryTransferToContainer(trappedThing, trappedInventory);
					} else {
						trappedInventory.TryAdd(trappedThing);
					}
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
			Scribe_Deep.Look(ref trappedInventory, "trappedInventory", null);
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
			if(mode == DestroyMode.KillFinalize && trappedInventory!=null) {
				trappedInventory.TryDropAll(Position, Map, ThingPlaceMode.Direct);
			}
			base.Destroy(mode);
		}

		private List<Thing> CrushThingsUnderWall(Thing wall) {
			var thingsList = wall.Map.thingGrid.ThingsListAt(wall.Position).Where(t => t != wall).ToList();
			foreach (var thing in thingsList) {
				var pawn = thing as Pawn;
				if (pawn != null && !pawn.RaceProps.IsMechanoid && !pawn.Dead) {
					foreach (var partRecord in pawn.RaceProps.body.GetPartsWithTag("BreathingSource")) {
						pawn.TakeDamage(new DamageInfo(Resources.Damage.FoamWallRekt, 9999, -1f, wall, partRecord));
					}
				} else if (thing.def.plant != null) {
					thing.Destroy(DestroyMode.KillFinalize);
				}
			}
			return wall.Map.thingGrid.ThingsListAt(wall.Position).Where(t => t != wall && (t.def.category == ThingCategory.Item || t.def.category == ThingCategory.Pawn)).ToList();
		}

		public override string GetInspectString() {
			if(trappedInventory!=null) {
				return string.Format("FoamWall_contents".Translate(), trappedInventory.ContentsString);
			}
			return "";
		}
	}
}
