using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace RemoteTech.Patches {
	/// <summary>
	/// Allows ISelectedThingPlaceWorker to be called with a reference to a Thing while it's selected.
	/// </summary>
	[HarmonyPatch(typeof(Thing), "DrawExtraSelectionOverlays", new Type[0])]
	internal class Thing_DrawExtraSelectionOverlays_Patch {
		private delegate void DrawGhostsMethod(PlaceWorker pw, ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing);
		private static bool patched;

		[HarmonyPrepare]
		public static void PrePatch() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!patched) RemoteTechController.Instance.Logger.Error($"{nameof(Thing_DrawExtraSelectionOverlays_Patch)} infix could not be applied.");
			});
		}

		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> DrawThingRefPlaceWorkers(IEnumerable<CodeInstruction> instructions) {
			patched = false;
			var requiredMethod = AccessTools.Method(typeof(PlaceWorker), "DrawGhost", new []{typeof(ThingDef), typeof(IntVec3), typeof(Rot4), typeof(Color)});
			foreach (var inst in instructions) {
				if (!patched && inst.opcode == OpCodes.Callvirt && requiredMethod != null && requiredMethod.Equals(inst.operand)) {
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, ((DrawGhostsMethod)DrawGhosts).Method);
					patched = true;
				} else {
					yield return inst;
				}
			}
		}

		private static void DrawGhosts(PlaceWorker pw, ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing) {
			if (pw is ISelectedThingPlaceWorker customWorker) {
				customWorker.DrawGhostForSelected(thing);
			} else {
				pw.DrawGhost(def, center, rot, ghostCol);
			}
		}
	}
}