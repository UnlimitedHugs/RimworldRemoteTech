using RimWorld;
using Verse;

namespace RemoteExplosives {
	// Receives detonation signals from CompWiredDetonationTransmitter and light explosives attached to parent thing.
	public class CompWiredDetonationReceiver : ThingComp {
		public void RecieveSignal(int additionalWickTicks) {
			var parentExplosive = parent as Building_RemoteExplosive;
			if(parentExplosive!=null && !parentExplosive.IsArmed) return;
		
			var customExplosive = parent.GetComp<CompCustomExplosive>();
			var vanillaExplosive = parent.GetComp<CompExplosive>();
			if (customExplosive != null) {
				customExplosive.StartWick(true, additionalWickTicks);
			}		
			if (vanillaExplosive != null) {
				vanillaExplosive.StartWick();	
			}
		}
	}
}