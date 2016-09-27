using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RemoteExplosives {
	/* 
	 * Replaces exploded charges with new blueprints that are forbidden for 30 seconds
	 * stores settings to give to charges once they have been rebuilt
	 */
	public class AutoReplaceWatcher : IExposable {
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

		public void ScheduleReplacement(CompAutoReplaceable replaceableComp) {
			var building = replaceableComp.parent;
			var blueprint = GenConstruct.PlaceBlueprintForBuild(building.def, replaceableComp.ParentPosition, replaceableComp.ParentRotation, Faction.OfPlayer, null);
			var entry = new ReplacementEntry {
				position = replaceableComp.ParentPosition,
				unforbidTick = Find.TickManager.TicksGame + replaceableComp.ForbiddenForTicks,
			};
			var explosive = building as Building_RemoteExplosive;
			if (explosive!=null) {
				entry.armed = explosive.IsArmed;
				entry.channel = explosive.CurrentChannel;
			}
			pendingSettings.Add(entry);
			if (replaceableComp.ForbiddenForTicks > 0) {
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
			Scribe_Collections.LookList(ref pendingSettings, "pendingSettings", LookMode.Deep);
			Scribe_Collections.LookList(ref pendingForbiddenBlueprints, "pendingForbiddenBlueprints", LookMode.Deep);
			if (pendingSettings == null) pendingSettings = new List<ReplacementEntry>();
			if (pendingForbiddenBlueprints == null) pendingForbiddenBlueprints = new List<ReplacementEntry>();
		}

		private void UnforbidScheduledBlueprints() {
			var currentTick = Find.TickManager.TicksGame;
			var anyHits = false;
			for (int i = 0; i < pendingForbiddenBlueprints.Count; i++) {
				var entry = pendingForbiddenBlueprints[i];
				if(entry.unforbidTick > currentTick) continue;
				var blueprint = Find.ThingGrid.ThingAt<Blueprint_Build>(entry.position);
				if (blueprint != null) {
					blueprint.SetForbidden(false, false);
				}
				anyHits = true;
			}
			if (anyHits) pendingForbiddenBlueprints.RemoveAll(e => e.unforbidTick <= currentTick);
		}

		// auto-placed blueprints may get cancelled
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