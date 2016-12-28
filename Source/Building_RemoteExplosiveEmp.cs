using Verse;
using Verse.Sound;

namespace RemoteExplosives {
	/*
	 * A remote explosive with a custom wind-up sound.
	 */
	public class Building_RemoteExplosiveEmp : Building_RemoteExplosive {
		private static readonly SoundDef chargeSound = SoundDef.Named("RemoteEmpCharge");
		private bool chargeSoundRequested;
		
		public Building_RemoteExplosiveEmp() {
			beepWhenLit = false;
		}

		public override void LightFuse() {
			if(!FuseLit) {
				chargeSoundRequested = true;
			}
			base.LightFuse();
		}

		public override void Tick() {
			base.Tick();
			if (chargeSoundRequested) {
				chargeSound.PlayOneShot(this);
				chargeSoundRequested = false;
			}
		}
	}
}
