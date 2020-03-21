using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Draws a radius ring based on the signal range stat
	/// </summary>
	public class PlaceWorker_DetonatorRadius : PlaceWorker {
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null) {
			if (thing != null) {
				// while existing building is selected
				(thing as ThingWithComps)?
					.GetComp<CompWirelessDetonationGridNode>()?
					.DrawRadiusRing(true);
			} else {
				// preparing to build
				var radiusStat = def.GetStatValueAbstract(Resources.Stat.rxSignalRange);
				if (radiusStat > 0f) {
					GenDraw.DrawRadiusRing(center, radiusStat);
				}
			}
		}
	}
}