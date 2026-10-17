using System;
using System.Collections.Generic;
using UnityEngine;

namespace Godlings
{
    public static class AttackPatterns
    {
        /// <summary>
        /// Snap aim to a cardinal direction independently of movement. At an
        /// exact diagonal tie, retain the previous direction if it is one of
        /// the tied directions, avoiding aim flicker on the stick boundary.
        /// </summary>
        public static Vector2Int SnapCardinal(Vector2 aim, Vector2Int previous)
        {
            if (aim == Vector2.zero) return previous;
            Vector2Int horizontal = new(Math.Sign(aim.x), 0);
            Vector2Int vertical = new(0, Math.Sign(aim.y));
            float x = Mathf.Abs(aim.x), y = Mathf.Abs(aim.y);
            if (x == y && (previous == horizontal || previous == vertical)) return previous;
            return x >= y ? horizontal : vertical;
        }

        public static Vector2Int[] Sword(Vector2Int origin, Vector2Int direction)
        {
            if (Math.Abs(direction.x) + Math.Abs(direction.y) != 1)
                throw new ArgumentException("Sword attacks require a cardinal direction.");
            return new[] { origin + direction, origin + direction * 2, origin + direction * 3 };
        }

        /// <summary>A continuous three-cell sword footprint follows the moving attacker.</summary>
        public static bool SwordContains(Vector2 origin, Vector2Int direction, Vector2 target, int reach = 3)
        {
            Vector2 difference = target-origin;
            float forward = Vector2.Dot(difference,direction);
            float sideways = Vector2.Dot(difference,new Vector2(-direction.y,direction.x));
            return reach > 0 && forward >= 0.5f && forward <= reach+0.5f && Mathf.Abs(sideways) <= 0.5f;
        }

        /// <summary>Six tiles across, two rows ahead; an even width straddles a cell edge.</summary>
        public static Vector2Int[] BossSwipe(Vector2Int origin, Vector2Int forward)
        {
            if (Math.Abs(forward.x)+Math.Abs(forward.y) != 1)
                throw new ArgumentException("Boss swipes require a cardinal direction.");
            var side = new Vector2Int(-forward.y,forward.x);
            var cells = new List<Vector2Int>(12);
            for (int depth=1;depth<=2;depth++)
                for (int across=-2;across<=3;across++)
                    cells.Add(origin+forward*depth+side*across);
            return cells.ToArray();
        }

        /// <summary>Warning and impact share a square footprint. Even sizes center between four tiles.</summary>
        public static Vector2Int[] Rock(Vector2Int center, int size = 3)
        {
            if (size < 1 || size > 3) throw new ArgumentOutOfRangeException(nameof(size));
            int first = -(size-1)/2;
            var cells = new List<Vector2Int>(size*size);
            for (int y = first; y < first+size; y++)
                for (int x = first; x < first+size; x++)
                    cells.Add(center + new Vector2Int(x, y));
            return cells.ToArray();
        }
    }
}
