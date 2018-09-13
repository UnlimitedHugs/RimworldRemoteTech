using System;
using System.Reflection;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// A wrapper for the proximity sensor settings, for easy change detection
	/// </summary>
	public class SensorSettings : IExposable, IEquatable<SensorSettings> {
		/// <summary>
		/// Assign values of differing fields between two objects to a third object.
		/// Reflection is fine, since this runs only when the settings dialog is closed.
		/// </summary>
		public static bool AssignModifiedFields(SensorSettings original, SensorSettings modified, SensorSettings destination) {
			var anyChanged = false;
			foreach (var field in typeof(SensorSettings).GetFields(BindingFlags.Public | BindingFlags.Instance)) {
				var originalVal = field.GetValue(original);
				var modifiedVal = field.GetValue(modified);
				if (originalVal != modifiedVal) {
					field.SetValue(destination, modifiedVal);
					anyChanged = true;
				}
			}
			return anyChanged;
		}

		public string Name = string.Empty;
		public float CooldownTime = 10f;
		public bool SendMessage = true;
		public bool AlternativeSound;
		public bool SendWired = true;
		public bool SendWireless;
		public bool DetectAnimals;
		public bool DetectFriendlies;
		public bool DetectEnemies;
		public void ExposeData() {
			Scribe_Values.Look(ref Name, "name");
			Scribe_Values.Look(ref CooldownTime, "cooldown", 10f);
			Scribe_Values.Look(ref SendMessage, "sendMessage", true);
			Scribe_Values.Look(ref AlternativeSound, "alternativeSound");
			Scribe_Values.Look(ref SendWired, "sendWired", true);
			Scribe_Values.Look(ref SendWireless, "sendWireless");
			Scribe_Values.Look(ref DetectAnimals, "detectAnimals");
			Scribe_Values.Look(ref DetectFriendlies, "detectFriendlies");
			Scribe_Values.Look(ref DetectEnemies, "detectEnemies");
		}
		public SensorSettings Clone() {
			return (SensorSettings)MemberwiseClone();
		}

		public bool Equals(SensorSettings other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return string.Equals(Name, other.Name) && CooldownTime.Equals(other.CooldownTime) && SendMessage == other.SendMessage && AlternativeSound == other.AlternativeSound && SendWired == other.SendWired && SendWireless == other.SendWireless && DetectAnimals == other.DetectAnimals && DetectFriendlies == other.DetectFriendlies && DetectEnemies == other.DetectEnemies;
		}

	}
}