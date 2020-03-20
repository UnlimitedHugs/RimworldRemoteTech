using RimWorld;
using Verse;

namespace RemoteTech {
	public class Alert_DetonatorWireFailure : Alert_Critical {
		private const float AutoExpireInSeconds = 8f;

		private static Alert_DetonatorWireFailure instance;
		public static Alert_DetonatorWireFailure Instance {
			get { return instance ?? (instance = new Alert_DetonatorWireFailure()); }
		}

		private int expireTick;
		private Fire wireFire;
		
		public Alert_DetonatorWireFailure() {
			instance = this;
		}
		
		public void ReportFailure(Fire createdFire) {
			expireTick = (int) (Find.TickManager.TicksGame + AutoExpireInSeconds*GenTicks.TicksPerRealSecond);
			wireFire = createdFire;
		}

		public override string GetLabel() {
			return "Alert_wireFailure_label".Translate();
		}

		public override TaggedString GetExplanation() {
			return "Alert_wireFailure_desc".Translate();
		}

		public override AlertReport GetReport() {
			var fireLive = wireFire != null && wireFire.Spawned;
			if (fireLive || expireTick > Find.TickManager.TicksGame) {
				return fireLive ? AlertReport.CulpritIs(wireFire) : AlertReport.Active;
			}
			return false;
		}
	}
}