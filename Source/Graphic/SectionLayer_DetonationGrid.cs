using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// An overlay for explosives and conductors on the wired detonation grid.
	/// Displays only when a building designator for a building with a relevant comp is selected.
	/// </summary>
	public class SectionLayer_DetonationGrid : SectionLayer_Things {
		private static readonly Type GridNodeCompType = typeof (CompDetonationGridNode);

		public SectionLayer_DetonationGrid(Section section) : base(section) {
			relevantChangeTypes = MapMeshFlag.Buildings;
		}

		private int lastCachedFrame;
		private bool cachedVisible;

		protected override void TakePrintFrom(Thing t) {
			var compThing = t as ThingWithComps;
			if(compThing == null) return;
			for (int i = 0; i < compThing.AllComps.Count; i++) {
				var comp = compThing.AllComps[i] as CompDetonationGridNode;
				if (comp == null) continue;
				comp.PrintForDetonationGrid(this);
			}
		}

		public override void DrawLayer() {
			// perform check only once per frame, cache result for other visible sections
			if (Time.frameCount > lastCachedFrame) {
				cachedVisible = false;
				var selectedDesignator = Find.DesignatorManager.SelectedDesignator;
				var designatorBuild = selectedDesignator as Designator_Build;
				var buildingDef = designatorBuild == null ? null : (designatorBuild.PlacingDef as ThingDef);
				cachedVisible = (buildingDef != null && DefHasGridComp(buildingDef)) || selectedDesignator is Designator_SelectDetonatorWire;
				lastCachedFrame = Time.frameCount;
			}
			if(!cachedVisible) return;
			base.DrawLayer();
		}

		private bool DefHasGridComp(ThingDef def) {
			var comps = def.comps;
			if (comps == null) return false;
			for (int i = 0; i < comps.Count; i++) {
				var compProps = comps[i];
				if (compProps != null && compProps.compClass != null && compProps.compClass.IsSubclassOf(GridNodeCompType)) 
					return true;
			}
			return false;
		}
	}
}