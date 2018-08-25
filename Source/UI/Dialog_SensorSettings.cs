using System;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	public class Dialog_SensorSettings : Window {
		public Action<SensorSettings> OnSettingsChanged;
		private readonly ISensorSettingsProvider sensor;
		private readonly SensorSettings settings;
		private string cooldownBuffer;

		public Dialog_SensorSettings(ISensorSettingsProvider sensor) {
			this.sensor = sensor;
			settings = sensor.Settings.Clone();
			absorbInputAroundWindow = false;
			doCloseX = true;
			doCloseButton = true;
			draggable = true;
		}

		public override Vector2 InitialSize {
			get {
				return new Vector2(450f, 400f);
			}
		}

		public override void DoWindowContents(Rect inRect) {
			var contentRect = inRect.AtZero();
			var l = new Listing_Standard { ColumnWidth = contentRect.width };
			l.Begin(contentRect);
			Text.Font = GameFont.Medium;
			l.Label("proxSensor_settings".Translate());
			Text.Font = GameFont.Small;
			l.Gap();

			Text.Anchor = TextAnchor.MiddleLeft;
			var entryRect = l.GetRect(26f);
			Widgets.Label(entryRect, "proxSensor_sName".Translate());
			settings.Name = Widgets.TextField(entryRect.RightHalf(), settings.Name);

			entryRect = l.GetRect(26f);
			Widgets.Label(entryRect, "proxSensor_sCooldown".Translate());
			Widgets.TextFieldNumeric(entryRect.RightHalf(), ref settings.CooldownTime, ref cooldownBuffer);
			Text.Anchor = TextAnchor.UpperLeft;

			l.CheckboxLabeled("proxSensor_sSendMessage".Translate(), ref settings.SendMessage);
			l.CheckboxLabeled("proxSensor_sSendWired".Translate(), ref settings.SendWired);
			l.Gap();
			GUI.enabled = sensor.HasWirelessUpgrade;
			l.Label("proxSensor_sWirelessUpgrade".Translate());
			l.CheckboxLabeled("proxSensor_sSendWireless".Translate(), ref settings.SendWireless);
			l.Gap();
			GUI.enabled = sensor.HasAIUpgrade;
			l.Label("proxSensor_sAIUpgrade".Translate());
			l.CheckboxLabeled("proxSensor_sDetectAnimals".Translate(), ref settings.DetectAnimals);
			l.CheckboxLabeled("proxSensor_sDetectFriendlies".Translate(), ref settings.DetectFriendlies);
			l.CheckboxLabeled("proxSensor_sDetectEnemies".Translate(), ref settings.DetectEnemies);
			GUI.enabled = true;
			l.End();
		}

		public override void PostClose() {
			base.PostClose();
			if (OnSettingsChanged != null && !settings.Equals(sensor.Settings)) OnSettingsChanged(settings);
		}
	}
}