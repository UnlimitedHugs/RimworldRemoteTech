using Verse;
using Verse.AI;

namespace RemoteTech {
	public class MentalBreakWorker_ColonistsOnly : MentalBreakWorker {
		public override bool BreakCanOccur(Pawn pawn) {
			return base.BreakCanOccur(pawn) && pawn.IsColonist;
		}
	}
}