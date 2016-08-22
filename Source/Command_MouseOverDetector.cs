using System;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	// A simple Command_Action that reports back when the mouse is hovering over it.
	public class Command_MouseOverDetector : Command_Action {
		public Action mouseOverCallback;

		public override GizmoResult GizmoOnGUI(Vector2 topLeft) {
			var rect = new Rect(topLeft.x, topLeft.y, Width, Height);
			if (Mouse.IsOver(rect) && mouseOverCallback!=null) {
				mouseOverCallback();
			}
			return base.GizmoOnGUI(topLeft);
		}
	}
}