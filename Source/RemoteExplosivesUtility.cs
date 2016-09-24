using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	// A place for common functions and utilities used by the mod.
	[StaticConstructorOnStartup]
	public static class RemoteExplosivesUtility {
		public const string InjectedRecipeNameSuffix = "Injected";

		// how long it will take to trigger an additional explosive
		private const int TicksBetweenTriggers = 2;
		private const string logPrefix = "[RemoteExplosives] ";

		private static readonly SoundDef UIChannelSound = SoundDef.Named("RemoteUIDialClick");
		private static readonly ResearchProjectDef channelsResearchDef = ResearchProjectDef.Named("RemoteExplosivesChannels");
		private static readonly KeyBindingDef nextChannelKeybindingDef = KeyBindingDef.Named("RemoteExplosivesNextChannel");
		private static readonly Texture2D[] UITex_Channels = new[] {
			ContentFinder<Texture2D>.Get("UIChannel0"),
			ContentFinder<Texture2D>.Get("UIChannel1"),
			ContentFinder<Texture2D>.Get("UIChannel2")
		};

		private static readonly string ChannelDialDesc = "RemoteExplosive_detonatorChannelChanger_desc".Translate();
		private static readonly string ChannelDialLabelBase = "RemoteExplosive_channelChanger_label".Translate();
		private static readonly string CurrenthannelLabelBase = "RemoteExplosive_currentChannel".Translate();

		private static DesignationDef desigationDef;

		public static DesignationDef SwitchDesigationDef {
			get { return desigationDef ?? (desigationDef = DefDatabase<DesignationDef>.GetNamed("RemoteExplosiveSwitch")); }
		}

		public enum RemoteChannel {
			White = 0,
			Red = 1,
			Green = 2
		}

		public static void Log(object message) {
			Verse.Log.Message(logPrefix + message);
		}

		public static void Error(object message) {
			Verse.Log.Error(logPrefix + message);
		}

		public static void UpdateSwitchDesignation(Thing thing) {
			if(thing == null || !(thing is ISwitchable)) return;
			var flag = (thing as ISwitchable).WantsSwitch();
			var designation = Find.DesignationManager.DesignationOn(thing, SwitchDesigationDef);
			if (flag && designation == null) {
				Find.DesignationManager.AddDesignation(new Designation(thing, SwitchDesigationDef));
			} else if (!flag && designation != null) {
				designation.Delete();
			}
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

		public static void LightArmedExplosivesInRange(IntVec3 center, float radius, RemoteChannel channel) {
			var armedExplosives = FindArmedExplosivesInRange(center, radius, channel);
			if (armedExplosives.Count > 0) {
				// closer ones will go off first
				armedExplosives = armedExplosives.OrderBy(e => e.Position.DistanceToSquared(center)).ToList();
				for (int i = 0; i < armedExplosives.Count; i++) {
					var explosive = armedExplosives[i];
					explosive.LightFuse(TicksBetweenTriggers * i);
				}
			} else {
				Messages.Message("Detonator_notargets".Translate(), MessageSound.Standard);
			}
		}

		public static List<Building_RemoteExplosive> FindArmedExplosivesInRange(IntVec3 center, float radius, RemoteChannel channel) {
			var results = new List<Building_RemoteExplosive>();
			var sample = Find.ListerBuildings.AllBuildingsColonistOfClass<Building_RemoteExplosive>();
			foreach (var explosive in sample) {
				if (explosive.IsArmed && explosive.CurrentChannel == channel && !explosive.FuseLit && TileIsInRange(explosive.Position, center, radius)) {
					results.Add(explosive);
				}
			}
			return results;
		}

		public static FloatMenuOption TryMakeDetonatorFloatMenuOption(Pawn forPawn, Thing detonator, Action onActivated) {
			if (!forPawn.Drafted) return null;
			var entry = new FloatMenuOption {
				action = onActivated,
				autoTakeable = false,
				Label = "Detonator_detonatenow".Translate(),
			};
			if (Find.Reservations.IsReserved(detonator, Faction.OfPlayer)) {
				entry.Disabled = true;
				var reservedByName = Find.Reservations.FirstReserverOf(detonator, Faction.OfPlayer).Name.ToStringShort;
				entry.Label += " " + "Detonator_detonatenow_reserved".Translate(reservedByName);
			}
			return entry;
		}

		private static bool TileIsInRange(IntVec3 tile1, IntVec3 tile2, float maxDistance) {
			return Mathf.Sqrt(Mathf.Pow(tile1.x - tile2.x, 2) + Mathf.Pow(tile1.z - tile2.z, 2)) <= maxDistance;
		}

		private static Texture2D GetUITextureForChannel(RemoteChannel channel) {
			return UITex_Channels[(int)channel];
		}
	}
}
