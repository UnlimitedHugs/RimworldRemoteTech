using System;
using System.Collections.Generic;
using HugsLib.Utils;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Holds the necessary data to run and display the wireless channel selector.
	/// </summary>
	public class CompChannelSelector : ThingComp, ISwitchable {
		public const string DesiredChannelChangedSignal = "DesiredChannelChanged";
		public const string ChannelChangedSignal = "ChannelChanged";

		private Action<int> gizmoCallback;
		private bool manualSwitching;
		private bool autoDraw;
		private RemoteExplosivesUtility.ChannelType gizmoMode;
		private CachedValue<Dictionary<int, List<IWirelessDetonationReceiver>>> channelPopulation;

		// saved
		private int _channel = 1;
		private int _desiredChannel = 1;

		public int Channel {
			get { return _channel; }
			set {
				_channel = value;
				UpdateSwitchDesignation();
			}
		}

		public int DesiredChannel {
			get { return _desiredChannel; }
			set {
				_desiredChannel = value;
				UpdateSwitchDesignation();
			}
		}

		public bool WantsSwitch() {
			return manualSwitching && DesiredChannel != Channel;
		}

		public void DoSwitch() {
			if (Channel != DesiredChannel) {
				Channel = DesiredChannel;
				parent.BroadcastCompSignal(ChannelChangedSignal);
			}
		}

		private void UpdateSwitchDesignation() {
			parent.UpdateSwitchDesignation();
		}

		public CompChannelSelector() {
			gizmoCallback = c => {
				DesiredChannel = c;
				if (!manualSwitching) DoSwitch();
				parent.BroadcastCompSignal(DesiredChannelChangedSignal);
			};
			channelPopulation = new CachedValue<Dictionary<int, List<IWirelessDetonationReceiver>>>(
				() => RemoteExplosivesUtility.FindReceiversInNetworkRange(parent)
			);
		}

		public CompChannelSelector Configure(bool manualChannelSwitching = false, bool autoDrawGizmo = false, RemoteExplosivesUtility.ChannelType gizmoType = RemoteExplosivesUtility.ChannelType.None) {
			manualSwitching = manualChannelSwitching;
			autoDraw = autoDrawGizmo;
			gizmoMode = gizmoType;
			return this;
		}

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Values.Look(ref _channel, "channel", 1);
			Scribe_Values.Look(ref _desiredChannel, "desiredChannel", 1);
		}

		public Gizmo GetChannelGizmo() {
			return RemoteExplosivesUtility.GetChannelGizmo(DesiredChannel, Channel, gizmoCallback, gizmoMode, channelPopulation);
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra() {
			if (autoDraw) {
				var g = GetChannelGizmo();
				if (g != null) yield return g;
			}
		}
	}
}