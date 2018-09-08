namespace RemoteTech {
	/// <summary>
	/// Provides a way for things and comps to carry over saved values during auto-replacement.
	/// Acts like IExposable- use AutoReplaceWatcher.ExposeValue to save stuff.
	/// </summary>
	public interface IAutoReplaceExposable {
		void ExposeAutoReplaceValues(AutoReplaceWatcher watcher);
	}
}