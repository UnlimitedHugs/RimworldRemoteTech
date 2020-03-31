﻿using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Stores custom negative goodwill caps with non-player factions.
	/// Lowering the cap circumvents the trader gassing exploit that allows to rob a trader, 
	/// release them, and end up with net positive faction goodwill in the end.
	/// </summary>
	public class CustomFactionGoodwillCaps : WorldComponent {
		public const int DefaultMinNegativeGoodwill = -100;
		public const int NegativeGoodwillCap = -5000;

		public static CustomFactionGoodwillCaps GetFromWorld() {
			return Find.World.GetComponent<CustomFactionGoodwillCaps>()
				?? throw new NullReferenceException($"Failed to get {nameof(CustomFactionGoodwillCaps)} from world");
		}
		
		private Dictionary<int, int> goodwillCaps = new Dictionary<int, int>();
		private HashSet<int> betrayedFactions = new HashSet<int>();

		public CustomFactionGoodwillCaps(World world) : base(world) {
		}
		
		public override void ExposeData() {
			Scribe_Collections.Look(ref goodwillCaps, "goodwillCaps", LookMode.Value, LookMode.Value);
			Scribe_Collections.Look(ref betrayedFactions, "betrayedFactions", LookMode.Value);
			if (Scribe.mode == LoadSaveMode.PostLoadInit) {
				if(goodwillCaps == null) goodwillCaps = new Dictionary<int, int>();
				if(betrayedFactions == null) betrayedFactions = new HashSet<int>();
			}
		}

		public void SetMinNegativeGoodwill(Faction faction, int minGoodwill) {
			goodwillCaps[faction.loadID] = Mathf.Max(NegativeGoodwillCap, minGoodwill);
		}

		public int GetMinNegativeGoodwill(Faction faction) {
			if (RemoteTechController.Instance.SettingLowerStandingCap.Value) {
				var factionId = faction.loadID;
				if (goodwillCaps.ContainsKey(factionId)) {
					return goodwillCaps[factionId];
				}
			}
			return DefaultMinNegativeGoodwill;
		}

		public bool HasPlayerBetrayedFaction(Faction faction) {
			return betrayedFactions.Contains(faction.loadID);
		}

		public void OnPlayerBetrayedFaction(Faction faction) {
			betrayedFactions.Add(faction.loadID);
		}
	}
}