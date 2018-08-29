using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Replaces destroyed buildings that have a CompAutoReplaceable new blueprints that are forbidden for a set number of seconds (see mod settings).
	/// Buildings and their comps can implement IAutoReplaceExposable to carry over additional data to their rebuilt form.
	/// </summary>
	/// <see cref="CompAutoReplaceable"/>
	/// <see cref="IAutoReplaceExposable"/>
	public class AutoReplaceWatcher : IExposable {
		private const int TicksBetweenSettingsPruning = GenTicks.TicksPerRealSecond;

		private class ReplacementEntry : IExposable {
			public IntVec3 position;
			public int unforbidTick;
			public Dictionary<string, ValueType> savedVars;

			public void ExposeData() {
				Scribe_Values.Look(ref position, "position");
				Scribe_Values.Look(ref unforbidTick, "unforbidTick");
				Scribe_Collections.Look(ref savedVars, "vars", LookMode.Value, LookMode.Value);
			}
		}

		private Map map;
		private Dictionary<string, ValueType> currentVars;

		// saved
		private List<ReplacementEntry> pendingSettings = new List<ReplacementEntry>();
		private List<ReplacementEntry> pendingForbiddenBlueprints = new List<ReplacementEntry>(); // acts as a queue for lack of queue saving

		public LoadSaveMode ExposeMode { get; private set; } = LoadSaveMode.Inactive;

		public void SetParentMap(Map parentMap) {
			map = parentMap;
		}

		public void ScheduleReplacement(CompAutoReplaceable replaceableComp) {
			var building = replaceableComp.parent;
			if (building?.def == null) return;
			if ((building.Stuff == null && building.def.MadeFromStuff) || (building.Stuff != null && !building.def.MadeFromStuff)) {
				RemoteExplosivesController.Instance.Logger.Warning("Could not schedule {0} auto-replacement due to Stuff discrepancy.", building);
				return;
			}
			var report = GenConstruct.CanPlaceBlueprintAt(building.def, replaceableComp.ParentPosition, replaceableComp.ParentRotation, map);
			if (!report.Accepted) {
				RemoteExplosivesController.Instance.Logger.Message($"Could not auto-replace {building.LabelCap}: {report.Reason}");
				return;
			}
			var blueprint = GenConstruct.PlaceBlueprintForBuild(building.def, replaceableComp.ParentPosition, map, replaceableComp.ParentRotation, Faction.OfPlayer, building.Stuff);
			var entry = new ReplacementEntry {
				position = replaceableComp.ParentPosition,
				unforbidTick = Find.TickManager.TicksGame + RemoteExplosivesController.Instance.BlueprintForbidDuration * GenTicks.TicksPerRealSecond,
				savedVars = new Dictionary<string, ValueType>()
			};
			InvokeExposableCallbacks(building, entry.savedVars, LoadSaveMode.Saving);
			pendingSettings.Add(entry);
			if (RemoteExplosivesController.Instance.BlueprintForbidDuration > 0) {
				blueprint.SetForbidden(true, false);
				pendingForbiddenBlueprints.Add(entry);
			}
		}

		public void OnReplaceableThingSpawned(ThingWithComps building) {
			for (int i = 0; i < pendingSettings.Count; i++) {
				var entry = pendingSettings[i];
				if (building.Position != entry.position) continue;
				if (entry.savedVars != null) {
					InvokeExposableCallbacks(building, entry.savedVars, LoadSaveMode.LoadingVars);
					InvokeExposableCallbacks(building, null, LoadSaveMode.PostLoadInit);
				}
				var replaceComp = building.TryGetComp<CompAutoReplaceable>();
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

		public bool ExposeValue<T>(ref T value, string name, T fallbackValue = default(T)) where T : struct {
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (ExposeMode == LoadSaveMode.Inactive) throw new InvalidOperationException("Values can only be exposed during IAutoReplaceExposable callbacks");
			if (ExposeMode == LoadSaveMode.LoadingVars) {
				if (currentVars.TryGetValue(name, out ValueType storedValue)) {
					value = (T)storedValue;
					//RemoteExplosivesController.Instance.Logger.Message($"Loaded {value} as {name}");
					return true;
				} else {
					value = fallbackValue;
					//RemoteExplosivesController.Instance.Logger.Message($"Loaded fallback {value} as {name}");
				}
			} else if (ExposeMode == LoadSaveMode.Saving) {
				//RemoteExplosivesController.Instance.Logger.Message($"Saving {value} as {name}");
				currentVars[name] = value;
				return true;
			}
			return false;
		}

		private void InvokeExposableCallbacks(ThingWithComps target, Dictionary<string, ValueType> vars, LoadSaveMode mode) {
			ExposeMode = mode;
			currentVars = vars;
			if(target is IAutoReplaceExposable t) t.ExposeAutoReplaceValues(this);
			foreach (var comp in target.AllComps) {
				if (comp is IAutoReplaceExposable c) c.ExposeAutoReplaceValues(this);
			}
			currentVars = null;
			ExposeMode = LoadSaveMode.Inactive;
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

		// auto-placed blueprints may get canceled. Clean entries up periodically
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