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

		private static readonly SoundDef UIChannelSound = SoundDef.Named("RemoteUIDialClick");
		private static readonly ResearchProjectDef channelsResearchDef = ResearchProjectDef.Named("RemoteExplosivesChannels");
		private static readonly KeyBindingDef nextChannelKeybindingDef = KeyBindingDef.Named("RemoteExplosivesNextChannel");
		private static readonly Texture2D[] UITex_Channels = {
			ContentFinder<Texture2D>.Get("UIChannel0"),
			ContentFinder<Texture2D>.Get("UIChannel1"),
			ContentFinder<Texture2D>.Get("UIChannel2")
		};

		private static readonly string ChannelDialDesc = "RemoteExplosive_detonatorChannelChanger_desc".Translate();
		private static readonly string ChannelDialLabelBase = "RemoteExplosive_channelChanger_label".Translate();
		private static readonly string CurrenthannelLabelBase = "RemoteExplosive_currentChannel".Translate();
		
		private static DesignationDef switchDesigationDef;
		public static DesignationDef SwitchDesigationDef {
			get { return switchDesigationDef ?? (switchDesigationDef = DefDatabase<DesignationDef>.GetNamed("RemoteExplosiveSwitch")); }
		}

		private static DesignationDef dryDesigationDef;
		public static DesignationDef DryOffDesigationDef {
			get { return dryDesigationDef ?? (dryDesigationDef = DefDatabase<DesignationDef>.GetNamed("DetonatorWireDryOff")); }
		}

		public enum RemoteChannel {
			White = 0,
			Red = 1,
			Green = 2
		}

		public static void UpdateSwitchDesignation(Thing thing) {
			var switchable = thing as ISwitchable;
			if(switchable == null) return;
			thing.ToggleDesignation(SwitchDesigationDef, switchable.WantsSwitch());
		}

		public static bool ChannelsUnlocked() {
			return channelsResearchDef.IsFinished;
		}

		public static RemoteChannel GetNextChannel(RemoteChannel channel) {
			const int totalChannels = 3;
			var nextChannel = Mathf.Clamp(((int)channel+1)%totalChannels, 0, totalChannels-1);
			return (RemoteChannel)nextChannel;
		}

		public static string GetChannelName(RemoteChannel channel) {
			return ("RemoteExplosive_channel" + ((int)channel)).Translate();
		}

		public static Command_Action MakeChannelGizmo(RemoteChannel currentChannel, Action activateCallback) {
			 return new Command_Action {
				action = activateCallback,
				icon = GetUITextureForChannel(currentChannel),
				activateSound = UIChannelSound,
				defaultDesc = ChannelDialDesc,
				defaultLabel = String.Format(ChannelDialLabelBase, GetChannelName(currentChannel)),
				hotKey = nextChannelKeybindingDef
			};
		}

		public static string GetCurrentChannelInspectString(RemoteChannel currentChannnel) {
			return String.Format(CurrenthannelLabelBase, GetChannelName(currentChannnel));
		}

		public static void LightArmedExplosivesInRange(IntVec3 center, Map map, float radius, RemoteChannel channel) {
			var armedExplosives = FindArmedExplosivesInRange(center, map, radius, channel);
			if (armedExplosives.Count > 0) {
				// closer ones will go off first
				armedExplosives = armedExplosives.OrderBy(e => e.Position.DistanceToSquared(center)).ToList();
				for (int i = 0; i < armedExplosives.Count; i++) {
					var explosive = armedExplosives[i];
					HugsLibController.Instance.CallbackScheduler.ScheduleCallback(explosive.LightFuse, TicksBetweenTriggers*i);
				}
			} else {
				Messages.Message("Detonator_notargets".Translate(), MessageSound.Standard);
			}
		}

		public static List<Building_RemoteExplosive> FindArmedExplosivesInRange(IntVec3 center, Map map, float radius, RemoteChannel channel) {
			var results = new List<Building_RemoteExplosive>();
			var sample = map.listerBuildings.AllBuildingsColonistOfClass<Building_RemoteExplosive>();
			foreach (var explosive in sample) {
				if (explosive.IsArmed && explosive.CurrentChannel == channel && !explosive.FuseLit && TileIsInRange(explosive.Position, center, radius)) {
					results.Add(explosive);
				}
			}
			return results;
		}

		public static FloatMenuOption TryMakeDetonatorFloatMenuOption(Pawn pawn, IPawnDetonateable detonator) {
			var detonatorThing = detonator as Thing;
			if (pawn == null || detonatorThing == null || !pawn.IsColonistPlayerControlled || pawn.drafter == null) return null;
			var entry = new FloatMenuOption {
				action = () => {
					if (!pawn.IsColonistPlayerControlled || !pawn.jobs.CanTakeOrderedJob()) return;
					if (!detonator.WantsDetonation) detonator.WantsDetonation = true;
					var job = new Job(DefDatabase<JobDef>.GetNamed(JobDriver_DetonateExplosives.JobDefName), detonatorThing);
					pawn.jobs.TryTakeOrderedJob(job);
				},
				autoTakeable = false,
				Label = "Detonator_detonatenow".Translate()
			};
			if (pawn.Map.reservationManager.IsReserved(detonatorThing, Faction.OfPlayer)) {
				entry.Disabled = true;
				var reservedByName = pawn.Map.reservationManager.FirstReserverOf(detonatorThing, Faction.OfPlayer).Name.ToStringShort;
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
					var cardianalRoof = roofGrid.RoofAt(cardinalCell);
					if (cardianalRoof == null || !cardianalRoof.isThickRoof) {
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
		
		private static bool TileIsInRange(IntVec3 tile1, IntVec3 tile2, float maxDistance) {
			return Mathf.Sqrt(Mathf.Pow(tile1.x - tile2.x, 2) + Mathf.Pow(tile1.z - tile2.z, 2)) <= maxDistance;
		}

		private static Texture2D GetUITextureForChannel(RemoteChannel channel) {
			return UITex_Channels[(int)channel];
		}
	}
}
