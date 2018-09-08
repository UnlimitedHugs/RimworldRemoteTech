using Verse;

namespace RemoteTech {
	public class SuperEMPDamageDef : DamageDef {
		public float incapHealthThreshold = .25f;
		public float incapChance = .33f;

		public SuperEMPDamageDef() {
			workerClass = typeof(DamageWorker_SuperEMP);
		}
	}
}