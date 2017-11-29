using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Harmony;
using HugsLib.Utils;
using RimWorld;
using Verse;

namespace RemoteExplosives.Patches {
	/// <summary>
	/// Allows faction standing to be reduced below the normal -100 limit
	/// See CustomFactionGoodwillCaps for details
	/// </summary>
	[HarmonyPatch(typeof(Faction), "AffectGoodwillWith", new []{typeof(Faction), typeof(float)})]
	internal class Faction_AffectGoodwillWith_Patch {
		private static bool patchApplied;

		[HarmonyPrepare]
		public static void Prepare() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!patchApplied) RemoteExplosivesController.Instance.Logger.Error("Faction_AffectGoodwillWith_Patch infix could not be applied");
			});
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> CustomNegativeStandingCap(IEnumerable<CodeInstruction> instructions) {
			foreach (var instruction in instructions) {
				if (!patchApplied && instruction.opcode == OpCodes.Ldc_R4 && (-100f).Equals(instruction.operand)) {
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, ((Func<Faction, float>)GetNegativeGoodwillCapForFaction).Method);
					patchApplied = true;
				} else {
					yield return instruction;
				}
			}
		}

		private static float GetNegativeGoodwillCapForFaction(Faction faction) {
			if (Current.ProgramState != ProgramState.Playing) {
				return CustomFactionGoodwillCaps.DefaultMinNegativeGoodwill;
			}
			return UtilityWorldObjectManager.GetUtilityWorldObject<CustomFactionGoodwillCaps>().GetMinNegativeGoodwill(faction);
		}
	}
}