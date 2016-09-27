using RimWorld;
using Verse;

namespace RemoteExplosives {
	public class Alert_DetonatorCordFailure : Alert_High {
		private const float AutoExpireInSeconds = 8f;

		private static Alert_DetonatorCordFailure instance;
		public static Alert_DetonatorCordFailure Instance {
			get { return instance ?? (instance = new Alert_DetonatorCordFailure()); }
		}

		private int expireTick;
		private Fire cordFire;
		
		public Alert_DetonatorCordFailure() {
			instance = this;
		}
		
		public void ReportFailue(Fire createdFire) {
			expireTick = (int) (Find.TickManager.TicksGame + AutoExpireInSeconds*GenTicks.TicksPerRealSecond);
			cordFire = createdFire;
		}

		public override string FullLabel {
			get { return "Alert_cordFailure_label".Translate(); }
		}

		public override string FullExplanation {
			get { return "Alert_cordFailure_desc".Translate(); }
		}

		public override AlertReport Report {
			get {
				var fireLive = cordFire != null && cordFire.Spawned;
				if (fireLive || expireTick > Find.TickManager.TicksGame) {
					return fireLive ? AlertReport.CulpritIs(cordFire) : AlertReport.Active;
				}
				return false;
			}
		}
	}
}