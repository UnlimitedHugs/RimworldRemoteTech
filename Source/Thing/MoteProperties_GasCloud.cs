// ReSharper disable UnassignedField.Global
using Verse;

namespace RemoteTech {
	public class MoteProperties_GasCloud : MoteProperties {
		/// <summary>
		/// Game ticks between gas ticks. Everything a gas cloud does is done during its gas tick.
		/// </summary>
		public int GastickInterval = 1; 
		/// <summary>
		/// Gas will attempt to multiply every x gas ticks.
		/// </summary>
		public int SpreadInterval = 1;
		/// <summary>
		/// How much concentration a cloud passes to its neighbors. A value of 1 will equalize concentrations with adjacent clouds on each gas tick.
		/// </summary>
		public float SpreadAmountMultiplier = 1f;
		/// <summary>
		/// When concentration is below this threshold, gas will not attempt to multiply.
		/// </summary>
		public float SpreadMinConcentration = 1f;
		/// <summary>
		/// Concentrations below this will make the sprite progressively transparent. 
		/// At this concentration the gas will have its full effect.
		/// </summary>
		public float FullAlphaConcentration = 1f;
		/// <summary>
		/// The amount to concentration lost on each gas tick when on a tile with a roof.
		/// </summary>
		public float RoofedDissipation;
		/// <summary>
		/// The amount to concentration lost under the open sky.
		/// </summary>
		public float UnroofedDissipation;
		/// <summary>
		/// How much the sprite will deviate from its base position and scale while animating.
		/// </summary>
		public float AnimationAmplitude;
		/// <summary>
		/// Duration of an animation cycle in seconds.
		/// </summary>
		public FloatRange AnimationPeriod;

		public virtual void PostLoad() {
			Assert(GastickInterval > 0, "GastickInterval must be greater than zero");
			Assert(SpreadInterval > 0, "SpreadInterval must be greater than zero");
			Assert(SpreadAmountMultiplier >= 0 && SpreadAmountMultiplier <= 1, "SpreadAmountMultiplier must be between 0 and 1");
			Assert(SpreadMinConcentration > 0, "SpreadMinConcentration must be greater than zero");
			Assert(FullAlphaConcentration > 0, "FullAlphaConcentration must be greater than zero");
			Assert(RoofedDissipation >= 0, "RoofedDissipation must be at least zero");
			Assert(UnroofedDissipation >= 0, "RoofedDissipation must be at least zero");
			Assert(AnimationAmplitude >= 0, "AnimationAmplitude must be at least zero");
		}

		protected void Assert(bool check, string errorMessage) {
			if(!check) Log.Error($"[RemoteTech] Invalid data in {GetType().Name} definition: {errorMessage}");
		}
	}
}
