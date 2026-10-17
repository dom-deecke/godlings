using System;
using System.Collections.Generic;
using UnityEngine;

namespace Godlings
{
    /// <summary>Loads the transcribed sketch, including markers, without generating rooms.</summary>
    [Serializable]
    public sealed class DemoLevel
    {
        public string[] rowsTopToBottom;
        [Serializable]
        public sealed class ArenaBounds
        {
            public int[] minimumCell;
            public int[] maximumCellInclusive;
        }
        public ArenaBounds bossArena;
        public ArenaBounds meteorRun, meteorApproach;
        public int meteorActivationColumn;
        public readonly HashSet<Vector2Int> Floor = new();
        public readonly HashSet<Vector2Int> Arena = new();
        public readonly HashSet<Vector2Int> MeteorZone = new();
        public readonly List<Vector2Int> EnemyStarts = new();
        public Vector2Int PlayerStart;
        public Vector2Int BossStart;

        public static DemoLevel Load(TextAsset source)
        {
            if (source == null) throw new ArgumentException("The sketch data is missing.");
            var level = JsonUtility.FromJson<DemoLevel>(source.text);
            if (level.rowsTopToBottom == null || level.rowsTopToBottom.Length == 0)
                throw new ArgumentException("The sketch must contain tile rows.");
            int width = level.rowsTopToBottom[0].Length;
            int players = 0, bosses = 0;
            for (int row = 0; row < level.rowsTopToBottom.Length; row++)
            {
                if (level.rowsTopToBottom[row].Length != width)
                    throw new ArgumentException("All sketch rows must have the same width.");
                for (int x = 0; x < width; x++)
                {
                    char marker = level.rowsTopToBottom[row][x];
                    Vector2Int cell = new(x, level.rowsTopToBottom.Length - 1 - row);
                    if (marker == '.') continue;
                    if (marker != '#' && marker != 'P' && marker != 'E' && marker != 'B')
                        throw new ArgumentException("Unknown sketch marker: " + marker);
                    level.Floor.Add(cell);
                    if (marker == 'P') { level.PlayerStart = cell; players++; }
                    if (marker == 'E') level.EnemyStarts.Add(cell);
                    if (marker == 'B') { level.BossStart = cell; bosses++; }
                }
            }
            if (players != 1 || bosses != 1) throw new ArgumentException("Expected one player and one boss marker.");
            if (level.bossArena == null || level.bossArena.minimumCell?.Length != 2
                || level.bossArena.maximumCellInclusive?.Length != 2)
                throw new ArgumentException("The sketch must define its boss arena bounds.");
            foreach (Vector2Int cell in level.Floor)
                if (cell.x >= level.bossArena.minimumCell[0] && cell.y >= level.bossArena.minimumCell[1]
                    && cell.x <= level.bossArena.maximumCellInclusive[0]
                    && cell.y <= level.bossArena.maximumCellInclusive[1]) level.Arena.Add(cell);
            level.MeteorZone.UnionWith(level.Arena);
            foreach (ArenaBounds region in new[] { level.meteorRun, level.meteorApproach })
                if (region?.minimumCell?.Length == 2 && region.maximumCellInclusive?.Length == 2)
                    foreach (Vector2Int cell in level.Floor)
                        if (cell.x >= region.minimumCell[0] && cell.y >= region.minimumCell[1]
                            && cell.x <= region.maximumCellInclusive[0] && cell.y <= region.maximumCellInclusive[1])
                            level.MeteorZone.Add(cell);
            return level;
        }

        public Vector2Int[] RockCenters()
        {
            var centers = new List<Vector2Int>();
            foreach (Vector2Int center in MeteorZone)
            {
                bool fits = true;
                foreach (Vector2Int cell in AttackPatterns.Rock(center))
                    if (!MeteorZone.Contains(cell)) { fits = false; break; }
                if (fits) centers.Add(center);
            }
            return centers.ToArray();
        }
    }
}
