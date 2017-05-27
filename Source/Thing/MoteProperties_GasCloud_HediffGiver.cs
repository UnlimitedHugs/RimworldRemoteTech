using System.Collections.Generic;
using Verse;

namespace RemoteExplosives {
	public class MoteProperties_GasCloud_HediffGiver : MoteProperties_GasCloud {
		public HediffDef hediffDef;
		public FloatRange hediffSeverityPerGastick;
		public bool requiresFleshyPawn;
		public List<ThingDef> immunizingApparelDefs; 
	}
}
