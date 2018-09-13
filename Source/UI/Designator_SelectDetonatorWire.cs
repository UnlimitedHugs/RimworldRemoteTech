using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// A designator that selects only detonation wire
	/// </summary>
	public class Designator_SelectDetonatorWire : Designator {
		public Designator_SelectDetonatorWire() {
			hotKey = KeyBindingDefOf.Misc10;
			icon = Resources.Textures.rxUISelectWire;
			useMouseIcon = true;
			defaultLabel = "WireDesignator_label".Translate();
			defaultDesc = "WireDesignator_desc".Translate();
			soundDragSustain = SoundDefOf.Designate_DragStandard;
			soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
			soundSucceeded = SoundDefOf.ThingSelected;
		}

		public override string Label {
			get { return "WireDesignator_label".Translate(); }
		}

		public override string Desc {
			get { return "WireDesignator_desc".Translate(); }
		}

		public override int DraggableDimensions {
			get { return 2; }
		}

		public override bool DragDrawMeasurements {
			get { return true; }
		}

		public override AcceptanceReport CanDesignateCell(IntVec3 loc) {
			var contents = Map.thingGrid.ThingsListAt(loc);
			if (contents != null) {
				for (int i = 0; i < contents.Count; i++) {
					if (IsWire(contents[i])) return true;
				}
			}
			return false;
		}
		
		public override void DesignateSingleCell(IntVec3 c) {
			if(!ShiftIsHeld()) Find.Selector.ClearSelection();
			CellDesignate(c);
			TryCloseArchitectMenu();
		}

		public override void DesignateMultiCell(IEnumerable<IntVec3> cells) {
			if (!ShiftIsHeld()) Find.Selector.ClearSelection();
			foreach (var cell in cells) {
				CellDesignate(cell);
			}
			TryCloseArchitectMenu();
		}

		private bool IsWire(Thing t) {
			return t.def != null && t.def.building is BuildingProperties_DetonatorWire;
		}

		private void CellDesignate(IntVec3 cell) {
			var contents = Map.thingGrid.ThingsListAt(cell);
			var selector = Find.Selector;
			if (contents != null) {
				for (int i = 0; i < contents.Count; i++) {
					var thing = contents[i];
					if (IsWire(thing) && !selector.SelectedObjects.Contains(thing)) {
						selector.SelectedObjects.Add(thing);
						SelectionDrawer.Notify_Selected(thing);
					}
				}
			}
		}

		private void TryCloseArchitectMenu() {
			if (Find.Selector.NumSelected == 0) return;
			if (Find.MainTabsRoot.OpenTab != MainButtonDefOf.Architect) return;
			Find.MainTabsRoot.EscapeCurrentTab();
		}

		private bool ShiftIsHeld() {
			return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		}
	}
}