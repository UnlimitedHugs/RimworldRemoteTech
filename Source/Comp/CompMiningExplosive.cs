using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteTech {
	/// <summary>
	/// An explosive with high power against rocks. Will break rocks within the defined area.
	/// </summary>
	public class CompMiningExplosive : CompCustomExplosive {
		private const int MinAffectedCellsToTriggerCaveInSound = 6;

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
					if (TryAffectThing(thing, parent, ref breakingPowerRemaining)) {
						affectedMineables++;
					}
				}
				if (breakingPowerRemaining <= 0) { // ran out of juice, stop breaking
					break;
				}
			}
			if (affectedMineables >= MinAffectedCellsToTriggerCaveInSound) {
				Resources.Sound.rxMiningCavein.PlayOneShot(new TargetInfo(parentPosition, parentMap));
			}
		}

		private bool TryAffectThing(Thing thing, Thing explosive, ref float breakingPowerRemaining) {
			if (thing.def == null || thing.Map == null) return false;
			var map = thing.Map;
			var affected = false;
			if (thing.def.mineable) {
				var rockBuildingDef = thing.def.building;
				if (rockBuildingDef == null) return false;
				var mineable = thing as Mineable;
				if (rockBuildingDef.isResourceRock && mineable != null) {
					// resource rocks
					breakingPowerRemaining -= thing.HitPoints * MiningProps.resourceBreakingCost;
					DamageResourceHolder(thing, explosive.GetStatValue(Resources.Stat.rxExplosiveMiningYield));
					BreakMineableAndYieldResources(mineable);
					affected = true;
				} else if (rockBuildingDef.isNaturalRock) {
					// stone
					breakingPowerRemaining -= thing.HitPoints;
					thing.Destroy();
					affected = true;
					if (thing.def.filthLeaving != null) {
						FilthMaker.MakeFilth(thing.Position, map, thing.def.filthLeaving, Rand.RangeInclusive(1, 3));
					}
					if (rockBuildingDef.mineableThing != null && Rand.Value < explosive.GetStatValue(Resources.Stat.rxExplosiveChunkYield)) {
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
				DamageResourceHolder(tree, explosive.GetStatValue(Resources.Stat.rxExplosiveWoodYield));
				var yield = tree.YieldNow();
				tree.PlantCollected();
				if (yield > 0) {
					var wood = ThingMaker.MakeThing(thing.def.plant.harvestedThingDef);
					wood.stackCount = yield;
					GenPlace.TryPlaceThing(wood, thing.Position, map, ThingPlaceMode.Direct);
				}
			}
			return affected;
		}

		// this affects the amount of ore drops
		private void DamageResourceHolder(Thing thing, float efficiency) {
			var damage = thing.MaxHitPoints * (1 - efficiency);
			thing.TakeDamage(new DamageInfo(DamageDefOf.Bomb, (int)damage, 0F, -1F, parent));
		}

		private void BreakMineableAndYieldResources(Mineable mineable) {
			// swiped from Mineable.TrySpawnYield
			// the vanilla system is hardcoded to work for pawns, so we implement our own copy
			if (mineable == null || mineable.def == null || mineable.def.building == null || mineable.def.building.mineableThing == null) {
				return;
			}
			var pos = mineable.Position;
			var map = mineable.Map;
			var hitPointsLeft = mineable.HitPoints;
			mineable.Destroy(DestroyMode.KillFinalize);
			// clean up the single-item stack left by the vanilla dropper
			var vanillaDrop = map.thingGrid.ThingAt(pos, mineable.def.building.mineableThing);
			if (vanillaDrop != null && !vanillaDrop.Destroyed) {
				vanillaDrop.Destroy();
			}
			if (Rand.Value > mineable.def.building.mineableDropChance) {
				return;
			}
			int yield = mineable.def.building.mineableYield;
			var yieldMultiplier = (hitPointsLeft / (float)mineable.MaxHitPoints);
			if (mineable.def.building.mineableYieldWasteable) {
				yield = Mathf.Max(1, GenMath.RoundRandom(yield * yieldMultiplier));
			}
			var drop = ThingMaker.MakeThing(mineable.def.building.mineableThing);
			drop.stackCount = yield;
			GenSpawn.Spawn(drop, pos, map);
		}
	}
}