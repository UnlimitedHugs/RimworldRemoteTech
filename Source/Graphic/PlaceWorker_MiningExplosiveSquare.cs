using System.Linq;
using Verse;

namespace RemoteExplosives {
	/* 
	 * A companion PlaceWorker for the clearing explosives. Needed to display the custom area overlay.
	 */
	public class PlaceWorker_MiningExplosiveSquare : PlaceWorker {
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot) {
			var compProps = (CompProperties_MiningExplosive)def.comps.FirstOrDefault(c => c is CompProperties_MiningExplosive);
			if (compProps == null) return;
			GenDraw.DrawFieldEdges(Building_MiningExplosiveSquare.GetAffectedCellsSquareAtPosition(center, compProps.miningRadius));
			base.DrawGhost(def, center, rot);
		}
	}
}
