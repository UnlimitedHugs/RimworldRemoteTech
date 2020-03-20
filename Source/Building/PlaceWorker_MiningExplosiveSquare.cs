using System.Linq;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// A companion PlaceWorker for the clearing explosives. Needed to display the custom area overlay.
	/// </summary>
	public class PlaceWorker_MiningExplosiveSquare : PlaceWorker {
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null) {
			var compProps = (CompProperties_MiningExplosive)def.comps.FirstOrDefault(c => c is CompProperties_MiningExplosive);
			if (compProps == null) return;
			GenDraw.DrawFieldEdges(Building_MiningExplosiveSquare.GetAffectedCellsSquareAtPosition(center, compProps.miningRadius));
			base.DrawGhost(def, center, rot, ghostCol);
		}
	}
}
