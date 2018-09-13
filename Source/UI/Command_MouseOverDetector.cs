using System;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// A simple Command_Action that reports back when the mouse is hovering over it.
	/// </summary>
	public class Command_MouseOverDetector : Command_Action {
		public Action mouseOverCallback;

		public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth) {
			var rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), Height);
			if (Mouse.IsOver(rect) && mouseOverCallback != null) {
				mouseOverCallback();
			}
			return base.GizmoOnGUI(topLeft, maxWidth);
		}
	}
}