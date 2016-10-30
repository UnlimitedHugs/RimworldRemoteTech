using System.Collections.Generic;
using Verse;
using System.Linq;

namespace RemoteExplosives {
	/* 
	 * A remote explosive that shows its range when selected.
	 */
	public class Building_MiningExplosive : Building_RemoteExplosive {

		private List<IntVec3> affectedCells;


		public override void SpawnSetup() {
			base.SpawnSetup();
			var comp = GetComp<CompMiningExplosive>();
			if (comp != null) {
				affectedCells = GetAffectedCellsAtPosition(Position, comp.MiningProps.miningRadius);
				comp.AssignCustomMiningArea(affectedCells);
			}
		}

		public override void DrawExtraSelectionOverlays() {
			base.DrawExtraSelectionOverlays();
			if (affectedCells != null) {
				GenDraw.DrawFieldEdges(affectedCells);
			}
		}

		internal virtual List<IntVec3> GetAffectedCellsAtPosition(IntVec3 position, float radius) {
			return GenRadial.RadialCellsAround(position, radius, true).ToList();
		}
	}
}
