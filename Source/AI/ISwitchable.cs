namespace RemoteExplosives {
	/// <summary>
	/// A replacement for the lost IFlickable in A13.
	/// Allows a Thing or Comp to call a colonist to perform a flick action.
	/// </summary>
	interface ISwitchable {
		bool WantsSwitch();
		void DoSwitch();
	}
}
