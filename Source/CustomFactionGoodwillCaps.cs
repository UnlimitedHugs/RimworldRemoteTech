using System.Collections.Generic;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Stores custom negative goodwill caps with non-player factions.
	/// Lowering the cap circumvents the trader gassing exploit that allows to rob a trader, 
	/// release them, and end up with net positive faction goodwill in the end.
	/// </summary>
	public class CustomFactionGoodwillCaps : UtilityWorldObject {
		public const int DefaultMinNegativeGoodwill = -100;
		public const int NegativeGoodwillCap = -5000;

		private Dictionary<int, int> goodwillCaps = new Dictionary<int, int>();

		public override void ExposeData() {
			base.ExposeData();
			Scribe_Collections.Look(ref goodwillCaps, "goodwillCaps", LookMode.Value, LookMode.Value);
		}

		public void SetMinNegativeGoodwill(Faction faction, int minGoodwill) {
			goodwillCaps[faction.loadID] = Mathf.Max(NegativeGoodwillCap, minGoodwill);
		}

		public int GetMinNegativeGoodwill(Faction faction) {
			var factionId = faction.loadID;
			if (goodwillCaps.ContainsKey(factionId)) {
				return goodwillCaps[factionId];
			}
			return DefaultMinNegativeGoodwill;
		}
	}
}