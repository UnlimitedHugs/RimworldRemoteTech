using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using Verse;

namespace RemoteTech.Patches {
	/// <summary>
	/// Prevents Sparkweed farms from generating in faction bases. Plants potatoes instead.
	/// </summary>
	[HarmonyPatch(typeof(SymbolResolver_CultivatedPlants), "DeterminePlantDef", new[] {typeof(CellRect)})]
	internal class CultivatedPlants_DeterminePlantDef_Patch {
		[HarmonyPostfix]
		public static void ReplaceSparkweedWithPotatoes(ref ThingDef __result) {
			if (__result == Resources.Thing.rxPlantSparkweed) {
				__result = ThingDefOf.Plant_Potato;
			}
		}
	}
}