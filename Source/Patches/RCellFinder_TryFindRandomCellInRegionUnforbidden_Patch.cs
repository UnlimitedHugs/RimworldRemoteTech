using System;
using Harmony;
using RemoteTech;
using RimWorld;
using Verse;

namespace AllowTool.Patches {
	/// <summary>
	/// Prevents friendly pawns from picking random cells that are listed in the in the <see cref="PlayerAvoidanceGrids"/>.
	/// </summary>
	[HarmonyPatch(typeof(RCellFinder), "TryFindRandomCellInRegionUnforbidden",
		new Type[] {typeof(Region), typeof(Pawn), typeof(Predicate<IntVec3>), typeof(IntVec3)},
		new ArgumentType[] {ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out})]
	internal static class RCellFinder_TryFindRandomCellInRegionUnforbidden_Patch {
		[HarmonyPrefix]
		public static void SkipPlayerAvoidGridCells(Pawn pawn, ref Predicate<IntVec3> validator) {
			var originalValidator = validator;
			validator = cell => (originalValidator == null || originalValidator(cell)) &&
								!PlayerAvoidanceGrids.PawnShouldAvoidCell(pawn, cell);
		}
	}
}