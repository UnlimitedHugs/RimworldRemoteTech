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
		private static readonly SoundDef caveinEffect = SoundDef.Named("RemoteMiningCavein");

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
			var area = customArea;
			if (area == null) {
				var radius = Mathf.Clamp(Mathf.Round(MiningProps.miningRadius), 0, 25);
				area = GenRadial.RadialCellsAround(parent.Position, radius, true).ToList();
			}
			var affectedMineables = 0;
			foreach (var pos in area) {
				var things = Find.ThingGrid.ThingsListAt(pos).ToArray(); // copy required because of collection modification
				foreach (var thing in things) {
					if (thing.def == null) continue;
					if (thing.def.mineable) {
						var rockBuildingDef = thing.def.building;
						if (rockBuildingDef == null) continue;
						if (rockBuildingDef.isResourceRock) {
							// resource rocks
							DamageResourceHolder(thing, MiningProps.resourceBreakingEfficiency);
							thing.Destroy(DestroyMode.Kill);
							affectedMineables++;
						} else if (rockBuildingDef.isNaturalRock) {
							// stone
							thing.Destroy();
							affectedMineables++;
							if (thing.def.filthLeaving != null) {
								FilthMaker.MakeFilth(thing.Position, thing.def.filthLeaving, Rand.RangeInclusive(1, 3));
							}
							if (rockBuildingDef.mineableThing != null && Rand.Value < MiningProps.rockChunkChance) {
								var rockDrop = ThingMaker.MakeThing(rockBuildingDef.mineableThing);
								if (rockDrop.def.stackLimit == 1) {
									rockDrop.stackCount = 1;
								} else {
									rockDrop.stackCount = Mathf.CeilToInt(rockBuildingDef.mineableYield);
								}
								GenPlace.TryPlaceThing(rockDrop, thing.Position, ThingPlaceMode.Direct);
							}
						} else {
							// all other mineables
							thing.Destroy(DestroyMode.Kill);
							affectedMineables++;
						}
					} else if (thing.def.plant != null && thing.def.plant.IsTree) {
						// trees
						var tree = (Plant) thing;
						DamageResourceHolder(tree, MiningProps.woodBreakingEfficiency);
						var yeild = tree.YieldNow();
						tree.PlantCollected();
						if (yeild > 0) {
							var wood = ThingMaker.MakeThing(thing.def.plant.harvestedThingDef);
							wood.stackCount = yeild;
							GenPlace.TryPlaceThing(wood, thing.Position, ThingPlaceMode.Direct);
						}
					}
				}
			}
			if (affectedMineables > 5) {
				caveinEffect.PlayOneShot(SoundInfo.InWorld(new TargetInfo(parent)));
			}
		}

		// this affects the amount of ore drops
		private void DamageResourceHolder(Thing thing, float efficiency) {
			var damage = thing.MaxHitPoints * (1 - efficiency);
			thing.TakeDamage(new DamageInfo(DamageDefOf.Bomb, (int)damage, parent));
		}
	}
}