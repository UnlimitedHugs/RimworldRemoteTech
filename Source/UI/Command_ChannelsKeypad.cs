using System;
using System.Collections.Generic;
using System.Linq;
using HugsLib.Utils;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RemoteTech {
	public class Command_ChannelsKeypad : Command_ChannelsBasic {
		private const int ButtonSize = 31;

		private readonly Action<int> activateCallback;
		private readonly Dictionary<int, List<IWirelessDetonationReceiver>> channelPopulation;
		private readonly int selectedChannel;
		private readonly bool switching;

		private int overChannel;
		private List<Command_ChannelsKeypad> groupedKeypads;

		public Command_ChannelsKeypad(int selectedChannel, bool switching, Action<int> activateCallback, Dictionary<int, List<IWirelessDetonationReceiver>> channelPopulation) : base(selectedChannel, switching, activateCallback) {
			this.selectedChannel = selectedChannel;
			this.switching = switching;
			this.activateCallback = activateCallback;
			this.channelPopulation = channelPopulation;
			totalChannels = 8;
			alsoClickIfOtherInGroupClicked = false;
		}

		public override void ProcessInput(Event ev) {
			activateSound?.PlayOneShotOnCamera();
			if (HugsLibUtility.ShiftIsHeld) {
				TrySelectChargesOnChannel(overChannel);
			} else {
				InvokeCallbacksForGroup(overChannel);
			}
		}

		public override float GetWidth(float maxWidth) {
			return 139f;
		}

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth) {
			const int rowSize = 4, padding = 6, gizmoHeight = 75, buttonSpacing = 1;
			var totalRect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), gizmoHeight);
			var contentRect = totalRect.ContractedBy(padding);
			Widgets.DrawWindowBackground(totalRect);

			overChannel = 0;
			for (int i = 0; i < 8; i++) {
				var channel = i + 1;
				var buttonRect = new Rect(contentRect.x + (i % rowSize) * (ButtonSize + buttonSpacing), contentRect.y + (i / rowSize) * (ButtonSize + buttonSpacing), ButtonSize, ButtonSize);
				Graphics.DrawTexture(buttonRect, Resources.Textures.rxUIChannelKeypadAtlas, Resources.Textures.KeypadAtlasCoords.Keys[i], ButtonSize, 0, 0, 0);
				var channelIsPopulated = channelPopulation != null && channelPopulation.ContainsKey(channel);
				var outline = Resources.Textures.KeypadAtlasCoords.OutlineOff;
				if (channel == selectedChannel) {
					outline = Resources.Textures.KeypadAtlasCoords.OutlineSelected;
				} else if (channelIsPopulated) {
					outline = Resources.Textures.KeypadAtlasCoords.OutlineHighlight;
				}
				Graphics.DrawTexture(buttonRect, Resources.Textures.rxUIChannelKeypadAtlas, outline, ButtonSize, 0, 0, 0);
				if (Mouse.IsOver(buttonRect)) {
					overChannel = channel;
					Widgets.DrawHighlight(buttonRect);
					if (channelIsPopulated) {
						// list number of charges of each type on this channel
						var tipText = channelPopulation[channel]
							.Select(r => r.LabelNoCount)
							.GroupBy(s => s)
							.OrderByDescending(g => g.Count())
							.Select(g => $"{g.Count()}x {g.Key}")
							.Join("\n");
						TooltipHandler.TipRegion(buttonRect, "RemoteExplosive_channelKeypad_buttonTip".Translate(tipText));
					}
				}
			}

			DrawLabel(totalRect);
			DrawHotkeyTip(totalRect);

			if (HotkeyWasPressed()) {
				activateSound?.PlayOneShotOnCamera();
				InvokeCallbacksForGroup(GetNextChannel(selectedChannel));
			}

			var state = GizmoState.Clear;
			if (Widgets.ButtonInvisible(totalRect)) {
				state = GizmoState.Interacted;
			} else if (Mouse.IsOver(totalRect)) {
				state = GizmoState.Mouseover;
			}
			return new GizmoResult(state);
		}

		private bool HotkeyWasPressed() {
			if (hotKey != null && hotKey.KeyDownEvent) {
				Event.current.Use();
				return true;
			}
			return false;
		}

		public override void MergeWith(Gizmo other) {
			// we can't use the default grouping mechanism (GizmoGridDrawer) because other group members don't know the last hovered button
			if(groupedKeypads == null) groupedKeypads = new List<Command_ChannelsKeypad>();
			var otherKeypad = other as Command_ChannelsKeypad;
			if (otherKeypad != null) {
				groupedKeypads.Add(otherKeypad);
			}
		}

		public override bool GroupsWith(Gizmo other) {
			var otherKeypad = other as Command_ChannelsKeypad;
			if (otherKeypad == null) return false;
			return otherKeypad.selectedChannel == selectedChannel && otherKeypad.switching == switching;
		}

		private void TrySelectChargesOnChannel(int channel) {
			List<IWirelessDetonationReceiver> charges = null;
			channelPopulation?.TryGetValue(channel, out charges);
			if (charges != null) {
				Find.Selector.ClearSelection();
				charges.ForEach(t => {
					if (t is Thing) Find.Selector.Select(t);
					if (t is ThingComp tc) Find.Selector.Select(tc.parent);
				});
			}
		}

		private void InvokeCallbacksForGroup(int channel) {
			if (channel > 0) {
				activateCallback?.Invoke(channel);
				groupedKeypads?.ForEach(kp => kp.activateCallback?.Invoke(channel));
			}
		}

		private void DrawHotkeyTip(Rect rect) {
			var hotkeyCode = hotKey?.MainKey ?? KeyCode.None;
			if (hotkeyCode != KeyCode.None && !GizmoGridDrawer.drawnHotKeys.Contains(hotkeyCode)) {
				var labelRect = new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, 20f);
				Widgets.Label(labelRect, hotkeyCode.ToStringReadable());
				GizmoGridDrawer.drawnHotKeys.Add(hotkeyCode);
			}
		}

		private void DrawLabel(Rect rect) {
			var label = GetLabelForChannel(selectedChannel, switching);
			var height = Text.CalcHeight(label, rect.width);
			var labelRect = new Rect(rect.x, rect.yMax - height + 12f, rect.width, height);
			GUI.DrawTexture(labelRect, TexUI.GrayTextBG);
			GUI.color = Color.white;
			Text.Anchor = TextAnchor.UpperCenter;
			Widgets.Label(labelRect, label);
			Text.Anchor = TextAnchor.UpperLeft;
			GUI.color = Color.white;
		}
	}
}