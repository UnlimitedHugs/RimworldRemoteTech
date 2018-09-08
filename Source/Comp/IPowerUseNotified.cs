namespace RemoteTech {
	/// <summary>
	/// For comps that want to be notified about their parent doing something that draws power.
	/// </summary>
	public interface IPowerUseNotified {
		void ReportPowerUse(float duration = 1f);
	}
}