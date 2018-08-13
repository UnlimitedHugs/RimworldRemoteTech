using Verse;

namespace RemoteExplosives {
	public class Building_RadioMast : Building {
		public override void DrawExtraSelectionOverlays() {
			base.DrawExtraSelectionOverlays();
			RemoteExplosivesUtility.DrawSelectedThingPlaceWorkerFor(this);
		}
	}
}