using Verse;

namespace RemoteTech {
	/// <summary>
	/// A CompGlower that can be toggled on and off
	/// </summary>
	public class CompGlowerToggleable : CompGlower {
		private float originalGlowRadius;

		public override void PostSpawnSetup(bool respawningAfterLoad) {
			base.PostSpawnSetup(respawningAfterLoad);
			originalGlowRadius = Props.glowRadius;
		}

		public void ToggleGlow(bool enable) {
			if (enable == originalGlowRadius.ApproximatelyEquals(Props.glowRadius)) return;
			Props.glowRadius = enable ? originalGlowRadius : 0f;
			// reset cache in parent class
			RemoteTechController.Instance.CompGlowerGlowOnField.SetValue(this, !(bool)RemoteTechController.Instance.CompGlowerShouldBeLitProperty.GetValue(this, null));
			UpdateLit(parent.Map);
		}
	}
}