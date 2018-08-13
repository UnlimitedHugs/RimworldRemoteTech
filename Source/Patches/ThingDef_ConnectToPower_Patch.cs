using Harmony;
using RimWorld;
using Verse;

namespace RemoteExplosives.Patches {
	/// <summary>
	/// Allows types extending CompPowerTrader to be recognized as power grid connectables
	/// </summary>
	[HarmonyPatch(typeof(ThingDef))]
	[HarmonyPatch("ConnectToPower", PropertyMethod.Getter)]
	internal class ThingDef_ConnectToPower_Patch {
		[HarmonyPostfix]
		public static void AllowPolymorphicComps(ThingDef __instance, ref bool __result) {
			if (!__instance.EverTransmitsPower) {
				for (var i = 0; i < __instance.comps.Count; i++) {
					if (typeof(CompPowerTrader).IsAssignableFrom(__instance.comps[i]?.compClass)) {
						__result = true;
						return;
					}
				}
			}
		}
	}
}