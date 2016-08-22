using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	// An explosive with high power against rocks. Will break rocks within the defined area.
	public class CompMiningExplosive : CompCustomExplosive {
		private static readonly SoundDef caveinEffect = SoundDef.Named("RemoteMiningCavein");
		
		private List<IntVec3> customArea;

		public CompProperties_MiningExplosive MiningProps {
			get {
				return (CompProperties_MiningExplosive) props;
			}
		}

		public void AssignCustomMiningArea(List<IntVec3> cells) {
			customArea = cells;
		}

		protected override void Detonate() {
			base.Detonate();
			var area = customArea;
			if(area==null) {
				var radius = Mathf.Clamp(Mathf.Round(MiningProps.miningRadius), 0, 25);
				area = GenRadial.RadialCellsAround(parent.Position, radius, true).ToList();
			}
			var affectedMineables = new List<Thing>();
			foreach (var pos in area) {
				var thing = MineUtility.MineableInCell(pos);
				if(thing!=null) affectedMineables.Add(thing);
			}
			foreach (var mineable in affectedMineables) {
				var rockBuildingDef = mineable.def.building;
				if(rockBuildingDef==null) continue;
				if(rockBuildingDef.isResourceRock) {
					var damage = mineable.MaxHitPoints*(1-MiningProps.resourceBreakingEfficiency);
					mineable.TakeDamage(new DamageInfo(DamageDefOf.Bomb, (int)damage, parent)); // this affects the amount of ore drops
					mineable.Destroy(DestroyMode.Kill);
				} else if(rockBuildingDef.isNaturalRock) {
					mineable.Destroy();
					if (mineable.def.filthLeaving != null) {
						FilthMaker.MakeFilth(mineable.Position, mineable.def.filthLeaving, Rand.RangeInclusive(1, 3));
					}
					if (rockBuildingDef.mineableThing != null && Rand.Value<MiningProps.rockChunkChance) {
						Thing rockDrop = ThingMaker.MakeThing(rockBuildingDef.mineableThing);
						if (rockDrop.def.stackLimit == 1) {
							rockDrop.stackCount = 1;
						} else {
							rockDrop.stackCount = Mathf.CeilToInt(rockBuildingDef.mineableYield);
						}
						GenPlace.TryPlaceThing(rockDrop, mineable.Position, ThingPlaceMode.Direct);
					}
				} else { 
					// all other mineables
					mineable.Destroy(DestroyMode.Kill);
				}
			}
			if (affectedMineables.Count > 5)
				caveinEffect.PlayOneShot(SoundInfo.InWorld(new TargetInfo(parent)));
		}
	}
}