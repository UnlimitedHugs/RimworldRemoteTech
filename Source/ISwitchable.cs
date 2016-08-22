// A replacement for the lost IFlickable in A13
namespace RemoteExplosives {
	interface ISwitchable {
		bool WantsSwitch();
		void DoSwitch();
	}
}
