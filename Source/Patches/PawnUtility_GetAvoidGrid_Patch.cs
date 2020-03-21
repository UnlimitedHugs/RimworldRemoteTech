using HarmonyLib;
using RimWorld;
using Verse;

namespace RemoteTech.Patches {
	/// <summary>
	/// Returns a pathfinding avoid grid for player colonists and other friendly non-animal pawns.
	/// Normally non-hostile factions are not granted the use of an avoid grid.
	/// </summary>
	[HarmonyPatch(typeof(PawnUtility), "GetAvoidGrid", new []{typeof(Pawn), typeof(bool)})]
	internal static class PawnUtility_GetAvoidGrid_Patch {
		[HarmonyPostfix]
		public static void ReturnFriendlyAvoidGrid(this Pawn p, ref ByteGrid __result) {
			if (__result == null && p?.Map != null && PlayerAvoidanceGrids.PawnHasPlayerAvoidanceGridKnowledge(p)) {
				__result = PlayerAvoidanceGrids.TryGetByteGridForMap(p.Map);
			}
		}
	}
}