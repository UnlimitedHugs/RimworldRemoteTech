using System;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	[StaticConstructorOnStartup]
	public static class RemoteExplosivesUtility {
		public static readonly string InjectedRecipeNameSuffix = "Injected";

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

		public static void UpdateSwitchDesignation(Thing thing) {
			if(thing == null || !(thing is ISwitchable)) return;
			bool flag = (thing as ISwitchable).WantsSwitch();
			Designation designation = Find.DesignationManager.DesignationOn(thing, SwitchDesigationDef);
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

		public static float QuinticEaseOut (float t, float start, float change, float duration) {
			t /= duration;
			t--;
			return change*(t*t*t*t*t + 1) + start;
		}

		private static Texture2D GetUITextureForChannel(RemoteChannel channel) {
			return UITex_Channels[(int)channel];
		}
	}
}
