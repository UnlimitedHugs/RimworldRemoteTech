using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using RimWorld;
using Verse;

namespace RemoteExplosives {
	/* 
	 * A wall that will kill and trap any pawns and items it is placed on.
	 * Contained items are dropped on destruction.
	 */
	public class Building_FoamWall : Mineable, IThingHolder {
		private bool justCreated;
		private ThingOwner<Thing> trappedInventory;
		private BuildingProperties_FoamWall wallProps;
		private ThingDef smoothingReplacementDef;

		public int SmoothWorkAmount {
			get { return wallProps.smoothWorkAmount; }
		}

		public override void SpawnSetup(Map map, bool respawningAfterLoad) {
			base.SpawnSetup(map, respawningAfterLoad);
			wallProps = def.building as BuildingProperties_FoamWall;
			if (wallProps == null) throw new Exception("Building_FoamWall requires BuildingProperties_FoamWall: " + def.defName);
			if(justCreated) {
				var trappedThings = CrushThingsUnderWall(this);
				if (trappedThings.Count == 0) return;
				if (trappedInventory == null) trappedInventory = new ThingOwner<Thing>(this, false);
				foreach (var trappedThing in trappedThings) {
					trappedInventory.TryAdd(trappedThing.SplitOff(trappedThing.stackCount));
				}
				justCreated = false;
			}
		}

		public ThingOwner GetDirectlyHeldThings() {
			return trappedInventory;
		}

		public void GetChildHolders(List<IThingHolder> outChildren) {
			ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
		}

		public override void PostMake() {
			base.PostMake();
			justCreated = true;
		}

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Deep.Look(ref trappedInventory, "trappedInventory", null);
			Scribe_Defs.Look(ref smoothingReplacementDef, "smoothingDef");
		}

		public override void Destroy(DestroyMode mode) {
			if(mode == DestroyMode.KillFinalize && trappedInventory != null) {
				trappedInventory.TryDropAll(Position, Map, ThingPlaceMode.Direct);
			}
			base.Destroy(mode);
		}

		public override IEnumerable<Gizmo> GetGizmos() {
			foreach (var gizmo in base.GetGizmos()) {
				yield return gizmo;
			}
			if (wallProps.smoothVariants.Count > 0) {
				yield return new Command_Action {
					icon = Resources.Textures.WallSmoothMenuIcon,
					action = () => Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>(
						wallProps.smoothVariants.Select(wallDef => new FloatMenuOption(wallDef.label.CapitalizeFirst(), () => {
							var selectedWalls = Find.Selector.SelectedObjects.OfType<Building_FoamWall>().Where(o => o.def == def);
							foreach (var wall in selectedWalls) {
								wall.smoothingReplacementDef = wallDef;
								wall.ToggleDesignation(Resources.Designation.rxFoamWallSmooth, true);	
							}
						}))
					))),
					defaultLabel = "FoamWall_smoothAction_label".Translate(),
					defaultDesc = "FoamWall_smoothAction_desc".Translate()
				};
			}
		}

		public void ApplySmoothing() {
			if (smoothingReplacementDef != null) {
				var map = Map;
				Destroy(DestroyMode.Vanish);
				var wallTile = GenSpawn.Spawn(smoothingReplacementDef, Position, map);
				wallTile.SetFactionDirect(Faction.OfPlayer);
				var foamWall = wallTile as Building_FoamWall;
				if (foamWall != null) {
					foamWall.trappedInventory = trappedInventory;
				}
			}
		}

		private List<Thing> CrushThingsUnderWall(Thing wall) {
			var thingsList = wall.Map.thingGrid.ThingsListAt(wall.Position).Where(t => t != wall).ToList();
			foreach (var thing in thingsList) {
				var pawn = thing as Pawn;
				if (pawn != null && !pawn.RaceProps.IsMechanoid && !pawn.Dead) {
					foreach (var partRecord in pawn.RaceProps.body.GetPartsWithTag(BodyPartTagDefOf.BreathingSource)) {
						pawn.TakeDamage(new DamageInfo(Resources.Damage.rxFoamWallStuck, 9999, 0f, -1f, wall, partRecord));
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
