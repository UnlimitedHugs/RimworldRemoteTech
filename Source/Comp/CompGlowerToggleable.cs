using Verse;

namespace RemoteExplosives {
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
			if (enable == (originalGlowRadius == Props.glowRadius)) return;
			Props.glowRadius = enable ? originalGlowRadius : 0f;
			// reset cache in parent class
			RemoteExplosivesController.Instance.CompGlowerGlowOnField.SetValue(this, !(bool)RemoteExplosivesController.Instance.CompGlowerShouldBeLitProperty.GetValue(this, null));
			UpdateLit(parent.Map);
		}
	}
}