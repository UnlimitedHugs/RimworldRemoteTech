using Verse;

namespace RemoteTech {
	/// <summary>
	/// Allows drawing place worker stuff with a reference to the selected thing.
	/// </summary>
	public interface ISelectedThingPlaceWorker {
		void DrawGhostForSelected(Thing thing);
	}
}