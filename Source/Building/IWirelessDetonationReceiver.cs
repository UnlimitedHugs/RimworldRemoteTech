using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// An interface for Things and ThingComps that want to be detected and triggered by the wireless detonation grid.
	/// </summary>
	public interface IWirelessDetonationReceiver {
		/// <summary>
		/// Used by the channel keypad to show the tooltip
		/// </summary>
		string LabelNoCount { get; }
		/// <summary>
		/// Used to determine adjacency and detonation order
		/// </summary>
		IntVec3 Position { get; }
		int CurrentChannel { get; }
		bool CanReceiveSignal { get; }
		void ReceiveSignal(Thing sender);
	}
}