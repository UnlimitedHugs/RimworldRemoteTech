using System.Linq;
using Verse;

namespace RemoteExplosives {
	public class PlaceWorker_MiningExplosiveSquare : PlaceWorker {
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot) {
			var compProps = (CompMiningExplosiveProperties)def.comps.FirstOrDefault(c => c is CompMiningExplosiveProperties);
			if (compProps == null) return;
			GenDraw.DrawFieldEdges(Building_MiningExplosiveSquare.GetAffectedCellsSquareAtPosition(center, compProps.miningRadius));
			base.DrawGhost(def, center, rot);
		}
	}
}
