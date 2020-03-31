using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RemoteTech.Patches {
	/// <summary>
	/// Allows faction standing to be reduced below the normal -100 limit
	/// See CustomFactionGoodwillCaps for details
	/// </summary>
	[HarmonyPatch(typeof(Faction), "TryAffectGoodwillWith", new []{typeof(Faction), typeof(int), typeof(bool), typeof(bool), typeof(string), typeof(GlobalTargetInfo)})]
	internal class Faction_AffectGoodwillWith_Patch {
		private static bool patchApplied;

		[HarmonyPrepare]
		public static void Prepare() {
			LongEventHandler.ExecuteWhenFinished(() => {
				if (!patchApplied) RemoteTechController.Instance.Logger.Error("Faction_AffectGoodwillWith_Patch infix could not be applied");
			});
		}

		[HarmonyTranspiler]
		public static IEnumerable<CodeInstruction> CustomNegativeStandingCap(IEnumerable<CodeInstruction> instructions) {
			patchApplied = false;
			foreach (var instruction in instructions) {
				if (!patchApplied && instruction.opcode == OpCodes.Ldc_I4_S && ((sbyte)-100).Equals(instruction.operand)) {
					yield return new CodeInstruction(OpCodes.Ldarg_0);
					yield return new CodeInstruction(OpCodes.Call, ((Func<Faction, int>)GetNegativeGoodwillCapForFaction).Method);
					patchApplied = true;
				} else {
					yield return instruction;
				}
			}
		}

		private static int GetNegativeGoodwillCapForFaction(Faction faction) {
			return Current.ProgramState != ProgramState.Playing
				? CustomFactionGoodwillCaps.DefaultMinNegativeGoodwill
				: CustomFactionGoodwillCaps.GetFromWorld().GetMinNegativeGoodwill(faction);
		}
	}
}