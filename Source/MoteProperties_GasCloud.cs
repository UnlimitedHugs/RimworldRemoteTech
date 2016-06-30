using Verse;

namespace RemoteExplosives {
	public class MoteProperties_GasCloud : MoteProperties {
		public int GastickInterval = 1; 
		public int SpreadInterval = 1;
		public float SpreadAmountMultiplier = 1f;
		public float SpreadMinConcentration = 1f;
		public float FullAlphaConcentration = 1f;
		public float RoofedDissipation;
		public float UnroofedDissipation;
		public float AnimationAmplitude;
		public FloatRange AnimationPeriod;

		public void PostLoad() {
			Assert(GastickInterval > 0, "GastickInterval must be greater than zero");
			Assert(SpreadInterval > 0, "SpreadInterval must be greater than zero");
			Assert(SpreadAmountMultiplier >= 0 && SpreadAmountMultiplier <= 1, "SpreadAmountMultiplier must be between 0 and 1");
			Assert(SpreadMinConcentration > 0, "SpreadMinConcentration must be greater than zero");
			Assert(FullAlphaConcentration > 0, "FullAlphaConcentration must be greater than zero");
			Assert(RoofedDissipation >= 0, "RoofedDissipation must be at least zero");
			Assert(UnroofedDissipation >= 0, "RoofedDissipation must be at least zero");
			Assert(AnimationAmplitude >= 0, "AnimationAmplitude must be at least zero");
		}

		private void Assert(bool check, string errorMessage) {
			if(!check) Log.Error("[RemoteExplosives] Invalid data in MoteProperties_GasCloud definition: "+errorMessage);
		}
	}
}
