using System;
using System.Collections.Generic;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Holds the necessary data to run and display the wireless channel selector.
	/// </summary>
	public class CompChannelSelector : ThingComp, ISwitchable, IAutoReplaceExposable {
		public const string DesiredChannelChangedSignal = "DesiredChannelChanged";
		public const string ChannelChangedSignal = "ChannelChanged";

		private Action<int> gizmoCallback;
		private bool manualSwitching;
		private bool autoDraw;
		private bool readPopulation;
		private RemoteTechUtility.ChannelType gizmoMode;
		private CachedValue<Dictionary<int, List<IWirelessDetonationReceiver>>> channelPopulation;

		// saved
		private int _channel = RemoteTechUtility.DefaultChannel;
		private int _desiredChannel = RemoteTechUtility.DefaultChannel;

		public int Channel {
			get { return _channel; }
			set {
				_channel = value;
				if (!manualSwitching) _desiredChannel = value;
				UpdateSwitchDesignation();
			}
		}

		public int DesiredChannel {
			get { return _desiredChannel; }
			set {
				_desiredChannel = value;
				if (!manualSwitching) _channel = value;
				UpdateSwitchDesignation();
			}
		}

		public Dictionary<int, List<IWirelessDetonationReceiver>> ChannelPopulation {
			get { return channelPopulation.Value; }
		}

		public bool WantsSwitch() {
			return manualSwitching && DesiredChannel != Channel;
		}

		public CompChannelSelector() {
			gizmoCallback = c => {
				DesiredChannel = c;
				if (!manualSwitching) DoSwitch();
				parent.BroadcastCompSignal(DesiredChannelChangedSignal);
			};
			channelPopulation = new CachedValue<Dictionary<int, List<IWirelessDetonationReceiver>>>(
				() => readPopulation ? RemoteTechUtility.FindReceiversInNetworkRange(parent) : null
			);
		}

		public CompChannelSelector Configure(bool manualChannelSwitching = false, bool autoDrawGizmo = false, bool canReadPopulation = false, RemoteTechUtility.ChannelType gizmoType = RemoteTechUtility.ChannelType.None) {
			manualSwitching = manualChannelSwitching;
			autoDraw = autoDrawGizmo;
			gizmoMode = gizmoType;
			readPopulation = canReadPopulation;
			return this;
		}

		public void DoSwitch() {
			if (Channel != DesiredChannel) {
				Channel = DesiredChannel;
				parent.BroadcastCompSignal(ChannelChangedSignal);
			}
		}

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Values.Look(ref _channel, "channel", RemoteTechUtility.DefaultChannel);
			Scribe_Values.Look(ref _desiredChannel, "desiredChannel", RemoteTechUtility.DefaultChannel);
		}

		public Gizmo GetChannelGizmo() {
			Dictionary<int, List<IWirelessDetonationReceiver>> population = gizmoMode == RemoteTechUtility.ChannelType.Advanced ? channelPopulation.Value : null;
			return RemoteTechUtility.GetChannelGizmo(DesiredChannel, Channel, gizmoCallback, gizmoMode, population);
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra() {
			if (autoDraw) {
				var g = GetChannelGizmo();
				if (g != null) yield return g;
			}
		}

		public void ExposeAutoReplaceValues(AutoReplaceWatcher watcher) {
			watcher.ExposeValue(ref _channel, "channel");
			if (watcher.ExposeMode == LoadSaveMode.LoadingVars) _desiredChannel = _channel;
		}

		private void UpdateSwitchDesignation() {
			parent.UpdateSwitchDesignation();
		}
	}
}