using RimWorld;
using Verse;

namespace RemoteExplosives {
	/*
	 * Receives detonation signals from CompWiredDetonationTransmitter and light explosives attached to parent thing.
	 */
	public class CompWiredDetonationReceiver : CompDetonationGridNode {
		public void RecieveSignal(int delayTicks) {
			var parentExplosive = parent as Building_RemoteExplosive;
			if(parentExplosive!=null && !parentExplosive.IsArmed) return;
		
			var customExplosive = parent.GetComp<CompCustomExplosive>();
			var vanillaExplosive = parent.GetComp<CompExplosive>();
			if (customExplosive != null) {
				CallbackScheduler.Instance.ScheduleCallback(() => customExplosive.StartWick(true), delayTicks);
			}		
			if (vanillaExplosive != null) {
				CallbackScheduler.Instance.ScheduleCallback(() => vanillaExplosive.StartWick(), delayTicks);	
			}
		}

		public override void PrintForDetonationGrid(SectionLayer layer) {
			PrintEndpoint(layer);
		}
	}
}