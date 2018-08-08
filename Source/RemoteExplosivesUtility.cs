using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RemoteExplosives {
	 /*
	  * A place for common functions and utilities used by the mod.
	  */
	[StaticConstructorOnStartup]
	public static class RemoteExplosivesUtility {
		public const string InjectedRecipeNameSuffix = "Injected";
		// how long it will take to trigger an additional explosive
		private const int TicksBetweenTriggers = 2;

		public enum ChannelType {
			None, Basic, Advanced
		}

		public static void UpdateSwitchDesignation(Thing thing) {
			var switchable = thing as ISwitchable;
			if(switchable == null) return;
			thing.ToggleDesignation(Resources.Designation.rxRemoteExplosiveSwitch, switchable.WantsSwitch());
		}

		public static ChannelType GetChannelsUnlockLevel() {
			if (Resources.Research.rxChannelsAdvanced.IsFinished) {
				return ChannelType.Advanced;
			} else if (Resources.Research.rxChannels.IsFinished) {
				return ChannelType.Basic;
			}
			return ChannelType.None;
		}

		public static Gizmo GetChannelGizmo(int desiredChannel, int currentChannel, Action<int> activateCallback, ChannelType gizmoType, Dictionary<int, List<Building_RemoteExplosive>> channelPopulation = null) {
			var switching = desiredChannel != currentChannel;
			if (gizmoType == ChannelType.Basic) {
				return new Command_ChannelsBasic(desiredChannel, switching, activateCallback);
			} else if (gizmoType == ChannelType.Advanced) {
				return new Command_ChannelsKeypad(desiredChannel, switching, activateCallback, channelPopulation);
			}
			return null;
		}

		public static string GetCurrentChannelInspectString(int currentChannel) {
			return "RemoteExplosive_currentChannel".Translate(currentChannel);
		}

		public static void LightArmedExplosivesInRange(IntVec3 center, Map map, float radius, int channel) {
			FindArmedExplosivesInRange(center, map, radius)
				.TryGetValue(channel,out List<Building_RemoteExplosive> armedExplosives);
			if (armedExplosives != null) {
				// closer ones will go off first
				armedExplosives = armedExplosives.OrderBy(e => e.Position.DistanceToSquared(center)).ToList();
				for (int i = 0; i < armedExplosives.Count; i++) {
					var explosive = armedExplosives[i];
					HugsLibController.Instance.TickDelayScheduler.ScheduleCallback(explosive.LightFuse, TicksBetweenTriggers*i, explosive);
				}
			} else {
				Messages.Message("Detonator_notargets".Translate(), MessageTypeDefOf.RejectInput);
			}
		}

		public static Dictionary<int, List<Building_RemoteExplosive>> FindArmedExplosivesInRange(IntVec3 center, Map map, float radius) {
			var results = new Dictionary<int, List<Building_RemoteExplosive>>();
			var sample = map.listerBuildings.AllBuildingsColonistOfClass<Building_RemoteExplosive>();
			foreach (var explosive in sample) {
				if (explosive.IsArmed && !explosive.FuseLit && TileIsInRange(explosive.Position, center, radius)) {
					results.TryGetValue(explosive.CurrentChannel, out List<Building_RemoteExplosive> list);
					if (list == null) {
						list = new List<Building_RemoteExplosive>();
						results[explosive.CurrentChannel] = list;
					}
					list.Add(explosive);
				}
			}
			return results;
		}

		public static FloatMenuOption TryMakeDetonatorFloatMenuOption(Pawn pawn, IPawnDetonateable detonator) {
			var detonatorThing = detonator as Thing;
			if (pawn == null || detonatorThing == null || !pawn.IsColonistPlayerControlled || pawn.drafter == null) return null;
			var entry = new FloatMenuOption("Detonator_detonatenow".Translate(), () => {
				if (!pawn.IsColonistPlayerControlled) return;
				if (!detonator.WantsDetonation) detonator.WantsDetonation = true;
				var job = new Job(Resources.Job.rxDetonateExplosives, detonatorThing);
				pawn.jobs.TryTakeOrderedJob(job);
			});
			if (pawn.Map.reservationManager.IsReservedAndRespected(detonatorThing, pawn)) {
				entry.Disabled = true;
				var reservedByName = pawn.Map.reservationManager.FirstRespectedReserver(detonatorThing, pawn).Name.ToStringShort;
				entry.Label += " " + "Detonator_detonatenow_reserved".Translate(reservedByName);
			}
			return entry;
		}

		// Determines if by being placed in the given cell the roof breaker has both a
		// thick roof within its radius, and a thin roof/no roof adjacent to it
		public static bool IsEffectiveRoofBreakerPlacement(float explosiveRadius, IntVec3 center, Map map) {
			if (explosiveRadius <= 0) return false;
			var roofGrid = map.roofGrid;
			var cardinals = GenAdj.CardinalDirections;
			var effectiveRadiusNumCells = GenRadial.NumCellsInRadius(explosiveRadius);
			var adjacentWeakRoofFound = false;
			var thickRoofInEffectiveRadius = false;
			for (int i = 0; i < effectiveRadiusNumCells; i++) {
				var radiusCell = center + GenRadial.RadialPattern[i];
				if (!radiusCell.InBounds(map)) continue;
				var roof = roofGrid.RoofAt(radiusCell);
				if (roof != null && roof.isThickRoof) {
					thickRoofInEffectiveRadius = true;
				}
				if (adjacentWeakRoofFound) continue;
				for (int j = 0; j < cardinals.Length; j++) {
					var cardinalCell = cardinals[j] + radiusCell;
					if (!cardinalCell.InBounds(map)) continue;
					var cardinalRoof = roofGrid.RoofAt(cardinalCell);
					if (cardinalRoof == null || !cardinalRoof.isThickRoof) {
						adjacentWeakRoofFound = true;
						break;
					}
				}
			}
			return thickRoofInEffectiveRadius && adjacentWeakRoofFound;
		}

		public static float TryGetExplosiveRadius(ThingDef def) {
			if (def == null || def.comps == null) return 0;
			for (int i = 0; i < def.comps.Count; i++) {
				var props = def.comps[i] as CompProperties_Explosive;
				if (props == null) continue;
				return props.explosiveRadius;
			}
			return 0;
		}

		public static GasCloud TryFindGasCloudAt(Map map, IntVec3 pos, ThingDef matchingDef = null) {
			if (!pos.InBounds(map)) return null;
			var thingList = map.thingGrid.ThingsListAtFast(map.cellIndices.CellToIndex(pos));
			for (int i = 0; i < thingList.Count; ++i) {
				var cloud = thingList[i] as GasCloud;
				if (cloud != null && (matchingDef == null || cloud.def == matchingDef)) return cloud;
			}
			return null;
		}

		public static void DeployGas(Map map, IntVec3 pos, ThingDef gasDef, int amount) {
			if (gasDef == null) {
				RemoteExplosivesController.Instance.Logger.Error("Tried to deploy null GasDef: " + Environment.StackTrace);
				return;
			}
			var cloud = TryFindGasCloudAt(map, pos, gasDef);
			if (cloud == null) {
				cloud = ThingMaker.MakeThing(gasDef) as GasCloud;
				if (cloud == null) {
					RemoteExplosivesController.Instance.Logger.Error(string.Format("Deployed thing was not a GasCloud: {0}", gasDef));
					return;
				}
				GenPlace.TryPlaceThing(cloud, pos, map, ThingPlaceMode.Direct);
			}
			cloud.ReceiveConcentration(amount);
		}

		public static CompUpgrade FirstUpgradeableComp(this Thing t) {
			if (t is ThingWithComps comps) {
				for (var i = 0; i < comps.AllComps.Count; i++) {
					if (comps.AllComps[i] is CompUpgrade comp && comp.WantsWork) {
						return comp;
					}
				}
			}
			return null;
		}

		public static CompUpgrade TryGetUpgrade(this Thing t, string upgradeReferenceId) {
			if (t is ThingWithComps comps) {
				for (var i = 0; i < comps.AllComps.Count; i++) {
					if (comps.AllComps[i] is CompUpgrade comp && comp.Props.referenceId == upgradeReferenceId) {
						return comp;
					}
				}
			}
			return null;
		}

		public static bool IsUpgradeCompleted(this Thing t, string upgradeReferenceId) {
			var upgrade = t.TryGetUpgrade(upgradeReferenceId);
			return upgrade != null && upgrade.Complete;
		}
		
		private static bool TileIsInRange(IntVec3 tile1, IntVec3 tile2, float maxDistance) {
			return Mathf.Sqrt(Mathf.Pow(tile1.x - tile2.x, 2) + Mathf.Pow(tile1.z - tile2.z, 2)) <= maxDistance;
		}
	}
}
