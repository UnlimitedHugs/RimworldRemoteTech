using Verse;

namespace RemoteExplosives {
	public interface IWirelessDetonationReceiver {
		string LabelNoCount { get; }
		IntVec3 Position { get; }
		int CurrentChannel { get; }
		bool CanReceiveSignal { get; }
		void ReceiveSignal(Thing sender);
	}
}