using RimWorld;

namespace RemoteTech {
	public class CompProperties_MiningExplosive : CompProperties_Explosive {
		public float miningRadius = 2f;
		public float breakingPower = 68400;
		public float resourceBreakingCost = 2f;
		public float woodBreakingCost = 2f;

		public CompProperties_MiningExplosive() {
			compClass = typeof(CompMiningExplosive);
		}
	}
}
