using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	/*
	 * A remote explosive with a custom wind-up sound.
	 */
	public class Building_RemoteExplosiveEmp : Building_RemoteExplosive {
		private static readonly SoundDef chargeSound = SoundDef.Named("RemoteEmpCharge");
		private Sustainer chargeSoundSustainer;
		
		public Building_RemoteExplosiveEmp() {
			beepWhenLit = false;
		}

		public override void LightFuse() {
			if(!FuseLit) {
				chargeSoundSustainer = new Sustainer(chargeSound, SoundInfo.InWorld(Position, MaintenanceType.PerTick));
			}
			base.LightFuse();
		}

		public override void Tick() {
			base.Tick();
			if (chargeSoundSustainer != null && FuseLit) {
				chargeSoundSustainer.Maintain();
			}
		}

		public override void Destroy(DestroyMode mode = DestroyMode.Vanish) {
			base.Destroy(mode);
			if (chargeSoundSustainer != null) {
				chargeSoundSustainer.End();
				chargeSoundSustainer = null;
			}
				
		}
	}
}
