namespace RemoteExplosives {
	/* 
	 * A replacement for the lost IFlickable in A13
	 */
	interface ISwitchable {
		bool WantsSwitch();
		void DoSwitch();
	}
}
