using System;
using System.Collections.Generic;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Destroyed buildings with this comp will be replaced as blueprints by AutoReplaceWatcher
	/// </summary>
	public class CompAutoReplaceable : ThingComp {
		private static readonly string AutoReplaceButtonLabel = "RemoteExplosive_autoReplace_label".Translate();
		private static readonly string AutoReplaceButtonDesc = "RemoteExplosive_autoReplace_desc".Translate();
		
		private bool autoReplaceEnabled;
		public bool AutoReplaceEnabled {
			get { return autoReplaceEnabled; }
			set { autoReplaceEnabled = value; }
		}
		public IntVec3 ParentPosition { get; private set; }
		public Rot4 ParentRotation { get; private set; }
		private bool autoDisplayGizmo = true;
		private bool wasLoaded;

		public override void PostExposeData() {
			base.PostExposeData();
			Scribe_Values.Look(ref autoReplaceEnabled, "enabled");
			if (Scribe.mode == LoadSaveMode.LoadingVars) wasLoaded = true;
		}

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			base.PostSpawnSetup(respawningAfterLoad);
			if(parent.def == null || parent.def.category!= ThingCategory.Building) throw new Exception("CompAutoReplaceable used on non-building");
			ParentPosition = parent.Position;
			ParentRotation = parent.Rotation;
			if (!wasLoaded) {
				parent.Map.GetComponent<MapComponent_RemoteTech>().ReplaceWatcher.OnReplaceableThingSpawned(parent);
			}
		}

		public override void PostDestroy(DestroyMode mode, Map map) {
			base.PostDestroy(mode, map);
			var replaceProps = props as CompProperties_AutoReplaceable;
			var applyOnVanish = replaceProps != null ? replaceProps.applyOnVanish : false;
			if (AutoReplaceEnabled && (mode == DestroyMode.KillFinalize || (applyOnVanish && mode == DestroyMode.Vanish))) {
				map.GetComponent<MapComponent_RemoteTech>().ReplaceWatcher.ScheduleReplacement(this);
			}
		}

		public CompAutoReplaceable DisableGizmoAutoDisplay() {
			autoDisplayGizmo = false;
			return this;
		}
		
		// get the gizmo to display it manually (for custom ordering)
		public Command MakeGizmo() {
			var replaceGizmo = new Command_Toggle {
				toggleAction = ReplaceGizmoAction,
				isActive = () => AutoReplaceEnabled,
				icon = Resources.Textures.rxUIAutoReplace,
				defaultLabel = AutoReplaceButtonLabel,
				defaultDesc = AutoReplaceButtonDesc,
				hotKey = Resources.KeyBinging.rxAutoReplace
			};
			return replaceGizmo;
		}

		public override IEnumerable<Gizmo> CompGetGizmosExtra() {
			if(autoDisplayGizmo) yield return MakeGizmo();
		}

		private void ReplaceGizmoAction() {
			AutoReplaceEnabled = !AutoReplaceEnabled;
		}
	}
}