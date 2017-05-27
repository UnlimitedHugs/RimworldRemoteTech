using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	/* 
	 * An explosive with high power against rocks. Will break rocks within the defined area.
	 */
	public class CompMiningExplosive : CompCustomExplosive {
		private const int MinAffectedCellsToTriggerCaveinSound = 6;
		protected static readonly SoundDef CaveInSoundEffect = SoundDef.Named("RemoteMiningCavein");

		private List<IntVec3> customArea;

		public CompProperties_MiningExplosive MiningProps {
			get {
				return (CompProperties_MiningExplosive)props;
			}
		}

		public void AssignCustomMiningArea(List<IntVec3> cells) {
			customArea = cells;
		}

		protected override void Detonate() {
			base.Detonate();
			if (parentMap == null) return;
			var area = customArea;
			if (area == null) {
				var radius = Mathf.Clamp(Mathf.Round(MiningProps.miningRadius), 0, 25);
				area = GenRadial.RadialCellsAround(parentPosition, radius, true).ToList();
			}
			var cellsByDistance = area.OrderBy(c => { // sort by distance from center
				var rel = c - parentPosition;
				return Mathf.Pow(rel.x, 2f) + Mathf.Pow(rel.z, 2f);
			});
			var affectedMineables = 0;
			var breakingPowerRemaining = MiningProps.breakingPower;
			foreach (var pos in cellsByDistance) {
				var things = parentMap.thingGrid.ThingsListAt(pos).ToArray(); // copy required because of collection modification
				foreach (var thing in things) {
					if (TryAffectThing(thing, ref breakingPowerRemaining)) {
						affectedMineables++;
					}
				}
				if (breakingPowerRemaining <= 0) { // ran out of juice, stop breaking
					break;
				}
			}
			if (affectedMineables >= MinAffectedCellsToTriggerCaveinSound) {
				CaveInSoundEffect.PlayOneShot(new TargetInfo(parentPosition, parentMap));
			}
		}

		private bool TryAffectThing(Thing thing, ref float breakingPowerRemaining) {
			if (thing.def == null || thing.Map == null) return false;
			var map = thing.Map;
			var affected = false;
			if (thing.def.mineable) {
				var rockBuildingDef = thing.def.building;
				if (rockBuildingDef == null) return false;
				if (rockBuildingDef.isResourceRock) {
					// resource rocks
					breakingPowerRemaining -= thing.HitPoints * MiningProps.resourceBreakingCost;
					DamageResourceHolder(thing, MiningProps.resourceBreakingYield);
					thing.Destroy(DestroyMode.KillFinalize);
					affected = true;
				} else if (rockBuildingDef.isNaturalRock) {
					// stone
					breakingPowerRemaining -= thing.HitPoints;
					thing.Destroy();
					affected = true;
					if (thing.def.filthLeaving != null) {
						FilthMaker.MakeFilth(thing.Position, map, thing.def.filthLeaving, Rand.RangeInclusive(1, 3));
					}
					if (rockBuildingDef.mineableThing != null && Rand.Value < MiningProps.rockChunkChance) {
						var rockDrop = ThingMaker.MakeThing(rockBuildingDef.mineableThing);
						if (rockDrop.def.stackLimit == 1) {
							rockDrop.stackCount = 1;
						} else {
							rockDrop.stackCount = Mathf.CeilToInt(rockBuildingDef.mineableYield);
						}
						GenPlace.TryPlaceThing(rockDrop, thing.Position, map, ThingPlaceMode.Direct);
					}
				} else {
					// all other mineables
					breakingPowerRemaining -= thing.HitPoints;
					thing.Destroy(DestroyMode.KillFinalize);
					affected = true;
				}
			} else if (thing.def.plant != null && thing.def.plant.IsTree) {
				// trees
				breakingPowerRemaining -= thing.HitPoints * MiningProps.woodBreakingCost;
				var tree = (Plant)thing;
				DamageResourceHolder(tree, MiningProps.woodBreakingYield);
				var yeild = tree.YieldNow();
				tree.PlantCollected();
				if (yeild > 0) {
					var wood = ThingMaker.MakeThing(thing.def.plant.harvestedThingDef);
					wood.stackCount = yeild;
					GenPlace.TryPlaceThing(wood, thing.Position, map, ThingPlaceMode.Direct);
				}
			}
			return affected;
		}

		// this affects the amount of ore drops
		private void DamageResourceHolder(Thing thing, float efficiency) {
			var damage = thing.MaxHitPoints * (1 - efficiency);
			thing.TakeDamage(new DamageInfo(DamageDefOf.Bomb, (int)damage, -1F, parent));
		}
	}
}