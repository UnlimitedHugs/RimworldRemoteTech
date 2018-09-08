using System.Linq;
using Verse;
using Verse.AI;

namespace RemoteTech {
	public class MentalStateWorker_RedButtonFever : MentalStateWorker {
		public override bool StateCanOccur(Pawn pawn) {
			return pawn.Map.listerBuildings.allBuildingsColonist.OfType<IRedButtonFeverTarget>().Any();
		}
	}
}