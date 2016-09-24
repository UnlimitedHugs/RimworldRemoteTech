using Verse;

namespace RemoteExplosives {
	public class CompPropertiesDamagedByPawn : CompProperties {
		public int damagePerTouch = 10;
		public int detectEveryTicks = 60;
		public EffecterDef touchEffect;
		public SoundDef touchSound;

		public CompPropertiesDamagedByPawn() {
			compClass = typeof (CompDamagedByPawn);
		}
	}
}