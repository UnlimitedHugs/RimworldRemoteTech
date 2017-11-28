using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RemoteExplosives {
	public class BuildingProperties_FoamWall : BuildingProperties {
		public List<ThingDef> smoothVariants = new List<ThingDef>();
		public int smoothWorkAmount;
	}
}