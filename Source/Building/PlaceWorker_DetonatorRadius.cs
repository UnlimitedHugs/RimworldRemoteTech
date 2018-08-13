using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	/// <summary>
	/// Draws a radius ring based on the signal range stat
	/// </summary>
	public class PlaceWorker_DetonatorRadius : PlaceWorker, ISelectedThingPlaceWorker {
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol) {
			var radiusStat = def.GetStatValueAbstract(Resources.Stat.rxSignalRange);
			if (radiusStat > 0f) {
				GenDraw.DrawRadiusRing(center, radiusStat);
			}
		}

		public void DrawGhostForSelected(Thing thing) {
			var radiusStat = thing.GetStatValue(Resources.Stat.rxSignalRange);
			if (radiusStat > 0f) {
				GenDraw.DrawRadiusRing(thing.Position, radiusStat);
			}
		}
	}
}