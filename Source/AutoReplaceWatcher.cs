using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RemoteExplosives {
	/* 
	 * Replaces exploded charges with new blueprints that are forbidden for a set number of seconds
	 * stores settings to give to charges once they have been rebuilt
	 */
	public class AutoReplaceWatcher : IExposable {
		private const int TicksBetweenSettingsPruning = GenTicks.TicksPerRealSecond;

		private class ReplacementEntry : IExposable {
			public IntVec3 position;
			public int unforbidTick;
			public bool armed;
			public RemoteExplosivesUtility.RemoteChannel channel;

			public void ExposeData() {
				Scribe_Values.Look(ref position, "position");
				Scribe_Values.Look(ref unforbidTick, "unforbidTick");
				Scribe_Values.Look(ref armed, "armed");
				Scribe_Values.Look(ref channel, "channel");
			}
		}

		private Map map;
		private List<ReplacementEntry> pendingSettings = new List<ReplacementEntry>();
		private List<ReplacementEntry> pendingForbiddenBlueprints = new List<ReplacementEntry>(); // acts as a queue for lack of queue saving
		
		public void SetParentMap(Map parentMap) {
			map = parentMap;
		}

		public void ScheduleReplacement(CompAutoReplaceable replaceableComp) {
			var building = replaceableComp.parent;
			if (building == null || building.def == null) return;
			if ((building.Stuff == null && building.def.MadeFromStuff) || (building.Stuff != null && !building.def.MadeFromStuff)) {
				RemoteExplosivesController.Instance.Logger.Warning("Could not schedule {0} auto-replacement due to Stuff discrepancy.", building);
				return;
			}
			var blueprint = GenConstruct.PlaceBlueprintForBuild(building.def, replaceableComp.ParentPosition, map, replaceableComp.ParentRotation, Faction.OfPlayer, building.Stuff);
			var entry = new ReplacementEntry {
				position = replaceableComp.ParentPosition,
				unforbidTick = Find.TickManager.TicksGame + RemoteExplosivesController.Instance.BlueprintForbidDuration * GenTicks.TicksPerRealSecond
			};
			var explosive = building as Building_RemoteExplosive;
			if (explosive!=null) {
				entry.armed = explosive.IsArmed;
				entry.channel = explosive.CurrentChannel;
			}
			pendingSettings.Add(entry);
			if (RemoteExplosivesController.Instance.BlueprintForbidDuration > 0) {
				blueprint.SetForbidden(true, false);
				pendingForbiddenBlueprints.Add(entry);
			}
		}

		public void TryApplySavedSettings(ThingWithComps explosive) {
			for (int i = 0; i < pendingSettings.Count; i++) {
				var entry = pendingSettings[i];
				if (explosive.Position != entry.position) continue;
				var remoteEx = explosive as Building_RemoteExplosive;
				if (remoteEx != null) {
					if (entry.armed) {
						remoteEx.Arm();
					}
					remoteEx.SetChannel(entry.channel);
				}
				var replaceComp = explosive.TryGetComp<CompAutoReplaceable>();
				if(replaceComp!=null) replaceComp.AutoReplaceEnabled = true;
				pendingSettings.RemoveAt(i);
				break;
			}
		}

		public void Tick() {
			UnforbidScheduledBlueprints();
			if (Find.TickManager.TicksGame%TicksBetweenSettingsPruning == 0) {
				PruneSettingsEntries();
			}
		}

		public void ExposeData() {
			Scribe_Collections.Look(ref pendingSettings, "pendingSettings", LookMode.Deep);
			Scribe_Collections.Look(ref pendingForbiddenBlueprints, "pendingForbiddenBlueprints", LookMode.Deep);
			if (pendingSettings == null) pendingSettings = new List<ReplacementEntry>();
			if (pendingForbiddenBlueprints == null) pendingForbiddenBlueprints = new List<ReplacementEntry>();
		}

		private void UnforbidScheduledBlueprints() {
			var currentTick = Find.TickManager.TicksGame;
			var anyEntriesExpired = false;
			for (int i = 0; i < pendingForbiddenBlueprints.Count; i++) {
				var entry = pendingForbiddenBlueprints[i];
				if(entry.unforbidTick > currentTick) continue;
				
				var blueprint = map.thingGrid.ThingAt<Blueprint_Build>(entry.position);
				if (blueprint != null) {
					blueprint.SetForbidden(false, false);
				}
				
				anyEntriesExpired = true;
			}
			if (anyEntriesExpired) pendingForbiddenBlueprints.RemoveAll(e => e.unforbidTick <= currentTick);
		}

		// auto-placed blueprints may get cancelled. Clean entries up periodically
		private void PruneSettingsEntries() {
			for (int i = pendingSettings.Count - 1; i >= 0; i--) {
				var entry = pendingSettings[i];
				bool containsBlueprint = false, containsBuildingFrame = false;
				if (map != null) {
					containsBlueprint = map.thingGrid.ThingAt<Blueprint_Build>(entry.position) != null;
					var edifice = map.edificeGrid[map.cellIndices.CellToIndex(entry.position)];
					containsBuildingFrame = edifice != null && edifice.def.IsFrame;
				}
				if (!containsBlueprint && !containsBuildingFrame) {
					pendingSettings.RemoveAt(i);
				}
			}
		}
	}
}