using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/*
	 * Destroyed buildings with this comp will be replaced as blueprints by AutoReplaceWatcher
	 */
	[StaticConstructorOnStartup]
	public class CompAutoReplaceable : ThingComp {
		private static readonly Texture2D UITex_AutoReplace = ContentFinder<Texture2D>.Get("UIAutoReplace");
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
			Scribe_Values.LookValue(ref autoReplaceEnabled, "enabled", false);
			if (Scribe.mode == LoadSaveMode.LoadingVars) wasLoaded = true;
		}

		public override void PostSpawnSetup() {
			base.PostSpawnSetup();
			if(parent.def == null || parent.def.category!= ThingCategory.Building) throw new Exception("CompAutoReplaceable used on non-building");
			if(parent.def.MadeFromStuff) throw new Exception("Buildings made from Stuff not supported for auto-replacement");
			ParentPosition = parent.Position;
			ParentRotation = parent.Rotation;
			if (!wasLoaded) {
				parent.Map.GetComponent<MapComponent_RemoteExplosives>().ReplaceWatcher.TryApplySavedSettings(parent);
			}
		}

		public override void PostDestroy(DestroyMode mode, Map map) {
			base.PostDestroy(mode, map);
			if (AutoReplaceEnabled && mode == DestroyMode.Kill) {
				map.GetComponent<MapComponent_RemoteExplosives>().ReplaceWatcher.ScheduleReplacement(this);
			}
		}

		public void DisableGizmoAutoDisplay() {
			autoDisplayGizmo = false;
		}
		
		// get the gizmo to display it manually (for custom ordering)
		public Command MakeGizmo() {
			var replaceGizmo = new Command_Toggle {
				toggleAction = ReplaceGizmoAction,
				isActive = () => AutoReplaceEnabled,
				icon = UITex_AutoReplace,
				defaultLabel = AutoReplaceButtonLabel,
				defaultDesc = AutoReplaceButtonDesc,
				hotKey = KeyBindingDef.Named("RemoteExplosiveAutoReplace")
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