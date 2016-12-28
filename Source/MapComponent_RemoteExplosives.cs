using HugsLib.Utils;
using Verse;

namespace RemoteExplosives {
	/**
	 * Stores the AutoReplaceWatcher for an individual map
	 */
	public class MapComponent_RemoteExplosives : MapComponent {
		
		private AutoReplaceWatcher replaceWatcher;

		public AutoReplaceWatcher ReplaceWatcher {
			get { return replaceWatcher; }
		}

		public MapComponent_RemoteExplosives(Map map) : base(map) {
			this.EnsureIsActive();
			replaceWatcher = new AutoReplaceWatcher();
			replaceWatcher.SetParentMap(map);
		}

		public override void ExposeData() {
			Scribe_Deep.LookDeep(ref replaceWatcher, "replaceWatcher");
			if(replaceWatcher == null) replaceWatcher = new AutoReplaceWatcher();
			replaceWatcher.SetParentMap(map);
		}
		
		public override void MapComponentTick() {
			base.MapComponentTick();
			replaceWatcher.Tick();
		}
	}
}