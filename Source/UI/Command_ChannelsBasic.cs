using System;
using UnityEngine;
using Verse;

namespace RemoteTech {
	[StaticConstructorOnStartup]
	public class Command_ChannelsBasic : Command {
		private readonly int selectedChannel;
		private readonly Action<int> activateCallback;
		protected int totalChannels = 3;

		private static readonly Texture2D[] UITex_ChannelsBasic = {
			Resources.Textures.rxUIChannelBasic1,
			Resources.Textures.rxUIChannelBasic2,
			Resources.Textures.rxUIChannelBasic3
		};

		public Command_ChannelsBasic(int selectedChannel, bool switching, Action<int> activateCallback) {
			this.selectedChannel = selectedChannel;
			this.activateCallback = activateCallback;
			icon = UITex_ChannelsBasic[Mathf.Clamp(selectedChannel - 1, 0, UITex_ChannelsBasic.Length - 1)];
			activateSound = Resources.Sound.rxDialClick;
			defaultDesc = "RemoteExplosive_detonatorChannelChanger_desc".Translate();
			defaultLabel = GetLabelForChannel(selectedChannel, switching);
			hotKey = Resources.KeyBinging.rxNextChannel;
		}

		public override void ProcessInput(Event ev) {
			base.ProcessInput(ev);
			activateCallback?.Invoke(GetNextChannel(selectedChannel));
		}

		protected string GetLabelForChannel(int channel, bool switching) {
			return "RemoteExplosive_channelChanger_label".Translate(channel, switching ? "RemoteExplosive_channel_switching".Translate() : "");
		}

		internal int GetNextChannel(int channel) {
			return Mathf.Clamp((channel + 1) % (totalChannels + 1), 1, totalChannels);
		}
	}
}