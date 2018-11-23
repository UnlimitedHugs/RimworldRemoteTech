using HugsLib.Utils;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Stores the AutoReplaceWatcher for an individual map
	/// </summary>
	public class MapComponent_RemoteTech : MapComponent {
		
		private AutoReplaceWatcher replaceWatcher;
		
		public ByteGrid PlayerAvoidGrid { get; }

		public AutoReplaceWatcher ReplaceWatcher {
			get { return replaceWatcher; }
		}

		public MapComponent_RemoteTech(Map map) : base(map) {
			this.EnsureIsActive();
			replaceWatcher = new AutoReplaceWatcher();
			replaceWatcher.SetParentMap(map);
			PlayerAvoidGrid = new ByteGrid(map);
		}

		public byte PlayerAvoidGridValueForCell(IntVec3 c) {
			return PlayerAvoidGrid[map.cellIndices.CellToIndex(c)];
		}

		public override void ExposeData() {
			Scribe_Deep.Look(ref replaceWatcher, "replaceWatcher");
			if(replaceWatcher == null) replaceWatcher = new AutoReplaceWatcher();
			replaceWatcher.SetParentMap(map);
		}
		
		public override void MapComponentTick() {
			base.MapComponentTick();
			replaceWatcher.Tick();
		}
	}
}