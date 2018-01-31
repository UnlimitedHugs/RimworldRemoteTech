// ReSharper disable UnassignedField.Global

using System.Collections.Generic;
using Verse;

namespace RemoteExplosives {
	public class SparkweedPlantDef : ThingDef {
		public int detectEveryTicks = 60;
		public float minimumIgnitePlantGrowth = .2f;
		public float ignitePlantChance = .5f;
		public float ignitePawnChance = .2f;
		public EffecterDef igniteEffecter;
		public List<ThingDef> ignitionSuppressorThings = new List<ThingDef>();
	}
}