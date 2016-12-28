using RimWorld;
using Verse;
// ReSharper disable UnassignedField.Global

namespace RemoteExplosives {
	[DefOf]
	public static class RemoteExplosivesDefOf {
		// sounds
		public static SoundDef RemoteDetonatorLever;
		public static SoundDef RemoteFoamSpray;
		public static SoundDef RemoteFoamSolidify;
		public static SoundDef RemoteExplosiveArmed;
		public static SoundDef RemoteExplosiveBeep;
		public static SoundDef RemoteChannelChange;

		// damage
		public static DamageDef FoamWallRekt;
		
		// work types
		public static WorkTypeDef Cleaning;
	}
}