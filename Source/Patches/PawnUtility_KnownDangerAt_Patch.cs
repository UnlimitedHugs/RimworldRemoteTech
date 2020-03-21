using HarmonyLib;
using RimWorld;
using Verse;

namespace RemoteTech.Patches {
	/// <summary>
	/// Specifies the friendly avoid grid as a known danger source to prevent colonists from wandering into marked cells
	/// </summary>
	[HarmonyPatch(typeof(PawnUtility), "KnownDangerAt", new []{typeof(IntVec3), typeof(Map), typeof(Pawn)})]
	internal static class PawnUtility_KnownDangerAt_Patch {
		[HarmonyPostfix]
		public static void ConsiderFriendlyAvoidGrid(IntVec3 c, Map map, Pawn forPawn, ref bool __result) {
			if (__result == false && PlayerAvoidanceGrids.PawnShouldAvoidCell(forPawn, c)) {
				__result = true;
			}
		}
	}
}