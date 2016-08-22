using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RemoteExplosives {
	// A mining explosive that affects an area of a square with the corners cut off.
	public class Building_MiningExplosiveSquare : Building_MiningExplosive {

		public static List<IntVec3> GetAffectedCellsSquareAtPosition(IntVec3 position, float radius) {
			var radiusInt = (int)Mathf.Clamp(Mathf.Round(radius), 0, 25);
			var finalCells = new List<IntVec3>();
			var corners = new List<IntVec3> {
				new IntVec3(position.x-radiusInt, 0, position.z-radiusInt),
				new IntVec3(position.x+radiusInt, 0, position.z-radiusInt),
				new IntVec3(position.x-radiusInt, 0, position.z+radiusInt),
				new IntVec3(position.x+radiusInt, 0, position.z+radiusInt),
			};
			var cellRect = new CellRect(position.x - radiusInt, position.z - radiusInt, radiusInt * 2 + 1, radiusInt * 2 + 1);
			if (radiusInt > 0) {
				foreach (var cell in cellRect) {
					if (!corners.Contains(cell)) finalCells.Add(cell);
				}
			} else {
				finalCells.Add(position);
			}
			return finalCells;
		}


		override internal List<IntVec3> GetAffectedCellsAtPosition(IntVec3 position, float radius) {
			return GetAffectedCellsSquareAtPosition(position, radius);
		}
	}
}
