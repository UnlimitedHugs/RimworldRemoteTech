using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/**
	 * A designator that selects only detonation cord
	 */
	[StaticConstructorOnStartup]
	public class Designator_SelectDetonatorCord : Designator {
		public static readonly Texture2D UISelectCord = ContentFinder<Texture2D>.Get("UISelectCord");
		
		public Designator_SelectDetonatorCord() {
			hotKey = KeyBindingDefOf.Misc10;
			icon = UISelectCord;
			useMouseIcon = true;
			defaultLabel = "CordDesignator_label".Translate();
			defaultDesc = "CordDesignator_desc".Translate();
			soundDragSustain = SoundDefOf.DesignateDragStandard;
			soundDragChanged = SoundDefOf.DesignateDragStandardChanged;
			soundSucceeded = SoundDefOf.ThingSelected;
		}

		public override string Label {
			get { return "CordDesignator_label".Translate(); }
		}

		public override string Desc {
			get { return "CordDesignator_desc".Translate(); }
		}

		public override int DraggableDimensions {
			get { return 2; }
		}

		public override bool DragDrawMeasurements {
			get { return true; }
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 loc) {
			var contents = Find.ThingGrid.ThingsListAt(loc);
			if (contents != null) {
				for (int i = 0; i < contents.Count; i++) {
					if (contents[i] is Building_DetonatorCord) return true;
				}
			}
			return false;
		}
		
		public override void DesignateSingleCell(IntVec3 c) {
			if(!ShiftIsHeld()) Find.Selector.ClearSelection();
			CellDesignate(c);
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells) {
			if (!ShiftIsHeld()) Find.Selector.ClearSelection();
			foreach (var cell in cells) {
				CellDesignate(cell);
			}
		}

		private void CellDesignate(IntVec3 cell) {
			var contents = Find.ThingGrid.ThingsListAt(cell);
			var selector = Find.Selector;
			if (contents != null) {
				for (int i = 0; i < contents.Count; i++) {
					var thing = contents[i];
					if (thing is Building_DetonatorCord && !selector.SelectedObjects.Contains(thing)) {
						selector.SelectedObjects.Add(thing);
						SelectionDrawer.Notify_Selected(thing);
					}
				}
			}
		}

		private bool ShiftIsHeld() {
			return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		}
	}
}