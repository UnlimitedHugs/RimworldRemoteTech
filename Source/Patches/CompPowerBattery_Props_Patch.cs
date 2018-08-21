using Harmony;
using RimWorld;

namespace RemoteExplosives.Patches {
	/// <summary>
	/// Allows comps extending CompPowerBattery to work properly with PowerNet
	/// </summary>
	[HarmonyPatch(typeof(CompPowerBattery))]
	[HarmonyPatch("Props", PropertyMethod.Getter)]
	internal class CompPowerBattery_Props_Patch {
		[HarmonyPostfix]
		public static void YieldCustomProps(CompPowerBattery __instance, ref CompProperties_Battery __result) {
			if (__instance is IBatteryPropsProvider i) {
				__result = i.ReplacementProps;
			}
		}
	}
}