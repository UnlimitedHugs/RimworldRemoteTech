using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	public static class RemoteExplosivesUtility {
		private static readonly SoundDef UIChannelSound = SoundDef.Named("RemoteUIDialClick");
		private static readonly ResearchProjectDef channelsResearchDef = ResearchProjectDef.Named("RemoteExplosivesChannels");
		private static readonly Texture2D[] UITex_Channels = new[] {
			ContentFinder<Texture2D>.Get("UIChannel0"),
			ContentFinder<Texture2D>.Get("UIChannel1"),
			ContentFinder<Texture2D>.Get("UIChannel2")
		};

		private static readonly string ChannelDialDesc = "RemoteExplosive_detonatorChannelChanger_desc".Translate();
		private static readonly string ChannelDialLabelBase = "RemoteExplosive_channelChanger_label".Translate();
		private static readonly string CurrenthannelLabelBase = "RemoteExplosive_currentChannel".Translate();

		public enum RemoteChannel {
			White = 0,
			Red = 1,
			Green = 2
		}

		public static void UpdateFlickDesignation(Thing thing) {
			if(thing == null || !(thing is IFlickable)) return;
			bool flag = (thing as IFlickable).WantsFlick();
			Designation designation = Find.DesignationManager.DesignationOn(thing, DesignationDefOf.Flick);
			if (flag && designation == null) {
				Find.DesignationManager.AddDesignation(new Designation(thing, DesignationDefOf.Flick));
			} else if (!flag && designation != null) {
				designation.Delete();
			}
		}

		public static bool ChannelsUnlocked() {
			return Find.ResearchManager.IsFinished(channelsResearchDef);
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
				defaultLabel = string.Format(ChannelDialLabelBase, GetChannelName(currentChannel))
			};
		}

		public static string GetCurrentChannelInspectString(RemoteChannel currentChannnel) {
			return String.Format(CurrenthannelLabelBase, GetChannelName(currentChannnel));
		}

		private static Texture2D GetUITextureForChannel(RemoteChannel channel) {
			return UITex_Channels[(int)channel];
		}
	}
}
