using System;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// A wrapper for the proximity sensor settings, for easy change detection
	/// </summary>
	public class SensorSettings : IExposable, IEquatable<SensorSettings> {
		public string Name = string.Empty;
		public float CooldownTime = 10f;
		public bool SendMessage = true;
		public bool SendWired = true;
		public bool SendWireless = true;
		public bool DetectAnimals = true;
		public bool DetectFriendlies = true;
		public bool DetectEnemies = true;
		public void ExposeData() {
			Scribe_Values.Look(ref Name, "name");
			Scribe_Values.Look(ref CooldownTime, "cooldown", 10f);
			Scribe_Values.Look(ref SendMessage, "sendMessage", true);
			Scribe_Values.Look(ref SendWired, "sendWired", true);
			Scribe_Values.Look(ref SendWireless, "sendWireless", true);
			Scribe_Values.Look(ref DetectAnimals, "detectAnimals", true);
			Scribe_Values.Look(ref DetectFriendlies, "detectFriendlies", true);
			Scribe_Values.Look(ref DetectEnemies, "detectEnemies", true);
		}
		public SensorSettings Clone() {
			return (SensorSettings)MemberwiseClone();
		}
		public bool Equals(SensorSettings other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return string.Equals(Name, other.Name) && CooldownTime.Equals(other.CooldownTime) && SendMessage == other.SendMessage && SendWired == other.SendWired && SendWireless == other.SendWireless && DetectAnimals == other.DetectAnimals && DetectFriendlies == other.DetectFriendlies && DetectEnemies == other.DetectEnemies;
		}
	}
}