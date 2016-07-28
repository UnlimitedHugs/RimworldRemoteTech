using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RemoteExplosives {
	// Replaces exploded charges with new blueprints that are forbidden for 30 seconds
	// stores settings to give to charges once they have been rebuilt
	public class AutoReplaceWatcher : IExposable {
		private const int BlueprintsForbiddenForTicks = 1800;
		private const int TicksBetweenSettingsPruning = 60;

		private class ReplacementEntry : IExposable {
			public IntVec3 position;
			public int unforbidTick;
			public bool armed;
			public RemoteExplosivesUtility.RemoteChannel channel;

			public void ExposeData() {
				Scribe_Values.LookValue(ref position, "position", new IntVec3());
				Scribe_Values.LookValue(ref unforbidTick, "unforbidTick", 0);
				Scribe_Values.LookValue(ref armed, "armed", false);
				Scribe_Values.LookValue(ref channel, "channel", RemoteExplosivesUtility.RemoteChannel.White);
			}
		}

		private List<ReplacementEntry> pendingSettings = new List<ReplacementEntry>();
		private List<ReplacementEntry> pendingForbiddenBlueprints = new List<ReplacementEntry>(); // acts as a queue for lack of queue saving

		public static AutoReplaceWatcher Instance { get; private set; }

		public AutoReplaceWatcher() {
			Instance = this;
		}

		public void ScheduleReplacement(Building_RemoteExplosive explosive) {
			//var canPlace = GenConstruct.CanPlaceBlueprintAt(explosive.def, explosive.Position, explosive.Rotation, false, explosive);
			//if(!canPlace.Accepted) return;
			var blueprint = GenConstruct.PlaceBlueprintForBuild(explosive.def, explosive.Position, explosive.Rotation, Faction.OfPlayer, null);
			blueprint.SetForbidden(true, false);
			var entry = new ReplacementEntry {
				position = explosive.Position,
				unforbidTick = Find.TickManager.TicksGame + BlueprintsForbiddenForTicks,
				armed = explosive.IsArmed,
				channel = explosive.CurrentChannel
			};
			pendingForbiddenBlueprints.Add(entry);
			pendingSettings.Add(entry);
		}

		public void TryApplySavedSettings(Building_RemoteExplosive explosive) {
			for (int i = 0; i < pendingSettings.Count; i++) {
				var entry = pendingSettings[i];
				if (explosive.Position == entry.position) {
					if (entry.armed) {
						explosive.Arm();
					}
					explosive.SetChannel(entry.channel);
					explosive.EnableAutoReplace();
					pendingSettings.RemoveAt(i);
					break;
				}
			}
		}

		public void Tick() {
			UnforbidScheduledBlueprints();
			if (Find.TickManager.TicksGame%TicksBetweenSettingsPruning == 0) {
				PruneSettingsEntries();
			}
		}

		public void ExposeData() {
			Scribe_Collections.LookList(ref pendingSettings, "pendingSettings", LookMode.Deep, new object[0]);
			Scribe_Collections.LookList(ref pendingForbiddenBlueprints, "pendingForbiddenBlueprints", LookMode.Deep, new object[0]);
			if (pendingSettings == null) pendingSettings = new List<ReplacementEntry>();
			if (pendingForbiddenBlueprints == null) pendingForbiddenBlueprints = new List<ReplacementEntry>();
		}

		private void UnforbidScheduledBlueprints() {
			var currentTick = Find.TickManager.TicksGame;
			while (pendingForbiddenBlueprints.Count > 0 && pendingForbiddenBlueprints[0].unforbidTick <= currentTick) {
				var entry = pendingForbiddenBlueprints[0];
				pendingForbiddenBlueprints.RemoveAt(0);
				var blueprint = Find.ThingGrid.ThingAt<Blueprint_Build>(entry.position);
				if (blueprint != null) {
					blueprint.SetForbidden(false, false);
				}
			}
		}

		private void PruneSettingsEntries() {
			for (int i = pendingSettings.Count - 1; i >= 0; i--) {
				var entry = pendingSettings[i];
				var containsBlueprint = Find.ThingGrid.ThingAt<Blueprint_Build>(entry.position) != null;
				var edifice = Find.EdificeGrid[CellIndices.CellToIndex(entry.position)];
				var containsBuildingFrame = edifice != null && edifice.def.IsFrame;
				if (!containsBlueprint && !containsBuildingFrame) {
					pendingSettings.RemoveAt(i);
				}
			}
		}
	}
}