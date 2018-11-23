using System;
using System.Collections.Generic;
using RemoteTech.Patches;
using RimWorld;
using UnityEngine;
using Verse;

namespace RemoteTech {
	/// <summary>
	/// Allows to maintain per-map avoidance grids for use by player pawns.
	/// This enables things to create no-go zones that only colonists and their allies know about.
	/// Buildings can already do this in vanilla, but this enables non-buildings to achieve the avoidance effect.
	/// Used patches: <see cref="PawnUtility_GetAvoidGrid_Patch"/>, <see cref="PawnUtility_KnownDangerAt_Patch"/>
	/// </summary>
	public static class PlayerAvoidanceGrids {

		private static readonly List<PlayerAvoidanceGrid> grids = new List<PlayerAvoidanceGrid>();

		public static void AddAvoidanceSource(Thing source, int pathCost) {
			AssertMap(source);
			pathCost = Mathf.Max(0, pathCost);
			if (!TryGetGridForMap(source.Map.uniqueID, out PlayerAvoidanceGrid grid)) {
				grid = new PlayerAvoidanceGrid(source.Map);
				grids.Add(grid);
			}
			var cellIndex = CellIndicesUtility.CellToIndex(source.Position, source.Map.Size.x);
			var previousCost = CalculatePathCostInCell(grid, cellIndex);
			var currentCost = Mathf.Min(byte.MaxValue, previousCost + pathCost);
			grid.byteGrid[cellIndex] = (byte)currentCost;
			grid.sources.Add(new AvoidanceSource(source.thingIDNumber, cellIndex, currentCost - previousCost));
		}

		public static void RemoveAvoidanceSource(Thing source) {
			AssertMap(source);
			if (!TryGetGridForMap(source.Map.uniqueID, out PlayerAvoidanceGrid grid)) return;
			var thingId = source.thingIDNumber;
			var sources = grid.sources;
			for (int i = sources.Count - 1; i >= 0; i--) {
				if (sources[i].thingId == thingId) sources.RemoveAt(i);
			}
			if (sources.Count == 0) {
				DiscardMap(source.Map);
			} else {
				var cellIndex = CellIndicesUtility.CellToIndex(source.Position, source.Map.Size.x);
				grid.byteGrid[cellIndex] = (byte)CalculatePathCostInCell(grid, cellIndex);
			}
		}

		public static ByteGrid TryGetByteGridForMap(Map map) {
			if (map == null || !TryGetGridForMap(map.uniqueID, out PlayerAvoidanceGrid grid)) return null;
			return grid.byteGrid;
		}

		public static bool ShouldAvoidCell(Map map, IntVec3 cell) {
			if (map == null || !cell.InBounds(map) || !TryGetGridForMap(map.uniqueID, out PlayerAvoidanceGrid grid)) return false;
			var cellIndex = CellIndicesUtility.CellToIndex(cell, map.Size.x);
			return grid.byteGrid[cellIndex] > 0;
		}

		public static void ClearAllMaps() {
			grids.Clear();
		}

		public static void DiscardMap(Map map) {
			if (map == null) throw new ArgumentNullException(nameof(map));
			for (var i = grids.Count - 1; i >= 0; i--) {
				if (grids[i].mapId == map.uniqueID) grids.RemoveAt(i);
			}
		}

		public static bool PawnHasPlayerAvoidanceGridKnowledge(Pawn p) {
			if (p == null) return false;
			var playerFaction = Faction.OfPlayer;
			return (p.Faction != null && !p.Faction.HostileTo(playerFaction)) 
				|| (p.guest != null && p.guest.Released)
				|| (p.IsPrisoner && p.HostFaction == playerFaction)
				|| (p.Faction == null && p.RaceProps.Humanlike);
		}

		private static bool TryGetGridForMap(int mapId, out PlayerAvoidanceGrid grid) {
			for (var i = 0; i < grids.Count; i++) {
				if (grids[i].mapId == mapId) {
					grid = grids[i];
					return true;
				}
			}
			grid = new PlayerAvoidanceGrid();
			return false;
		}

		private static void AssertMap(Thing source) {
			if (source.Map == null) throw new ArgumentException("Source thing does not belong to a map: " + source);
		}

		private static int CalculatePathCostInCell(PlayerAvoidanceGrid grid, int cellIndex) {
			var sources = grid.sources;
			var pathCostSum = 0;
			for (int i = 0; i < sources.Count; i++) {
				if (sources[i].cellIndex == cellIndex) pathCostSum += sources[i].addedCost;
			}
			return pathCostSum;
		}

		private struct PlayerAvoidanceGrid {
			public readonly int mapId;
			public readonly ByteGrid byteGrid;
			public readonly List<AvoidanceSource> sources;

			public PlayerAvoidanceGrid(Map map) {
				mapId = map.uniqueID;
				byteGrid = new ByteGrid(map);
				sources = new List<AvoidanceSource>();
			}
		}

		private struct AvoidanceSource {
			public readonly int thingId;
			public readonly int cellIndex;
			public readonly int addedCost;

			public AvoidanceSource(int thingId, int cellIndex, int addedCost) {
				this.thingId = thingId;
				this.cellIndex = cellIndex;
				this.addedCost = addedCost;
			}
		}
	}
}