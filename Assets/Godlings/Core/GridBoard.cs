using System;
using System.Collections.Generic;
using UnityEngine;

namespace Godlings
{
    /// <summary>
    /// Owns walkable cells and actor occupancy. The supplied level sketch will
    /// determine the floor; this class never generates a level layout.
    /// </summary>
    public sealed class GridBoard
    {
        private readonly HashSet<Vector2Int> floor;
        private readonly Dictionary<string, Vector2Int> positions = new();
        private readonly Dictionary<string, Vector2Int> destinations = new();

        public GridBoard(IEnumerable<Vector2Int> walkableCells)
        {
            floor = new HashSet<Vector2Int>(walkableCells);
        }

        // A crater changes traversability, never the underlying level layout.
        private readonly Dictionary<Vector2Int,double> blockedUntil = new();
        private double gameplayTime;

        public void AdvanceTime(double now)
        {
            if (now < gameplayTime) throw new ArgumentException("Gameplay time cannot move backwards.");
            gameplayTime = now;
        }

        public void BlockCells(IEnumerable<Vector2Int> cells, double expiresAt)
        {
            foreach (Vector2Int cell in cells)
                if (floor.Contains(cell))
                    blockedUntil[cell] = blockedUntil.TryGetValue(cell,out double previous)
                        ? Math.Max(previous,expiresAt) : expiresAt;
        }

        public bool IsWalkable(Vector2Int cell) => IsFloor(cell)
            && (!blockedUntil.TryGetValue(cell,out double expiry) || gameplayTime >= expiry);

        public bool IsFloor(Vector2Int cell) => floor.Contains(cell);
        public Vector2Int PositionOf(string actor) => positions[actor];

        public bool CanCross(Vector2Int origin, Vector2Int direction)
        {
            if (direction == Vector2Int.zero || Math.Abs(direction.x) > 1 || Math.Abs(direction.y) > 1)
                return false;
            return IsWalkable(origin + direction) && (direction.x == 0 || direction.y == 0
                || (IsWalkable(origin + new Vector2Int(direction.x,0))
                    && IsWalkable(origin + new Vector2Int(0,direction.y))));
        }

        public bool ActorOccupies(string actor, Vector2Int cell) =>
            (positions.TryGetValue(actor,out var origin) && origin == cell)
            || (destinations.TryGetValue(actor,out var target) && target == cell);

        public bool IsOccupied(Vector2Int cell)
        {
            return positions.ContainsValue(cell) || destinations.ContainsValue(cell);
        }

        public void AddActor(string actor, Vector2Int cell)
        {
            if (string.IsNullOrWhiteSpace(actor) || positions.ContainsKey(actor))
                throw new ArgumentException("Actor identity must be nonempty and unique.");
            if (!IsFloor(cell) || IsOccupied(cell))
                throw new ArgumentException("Actor must start on an unoccupied floor cell.");
            positions.Add(actor, cell);
        }

        /// <summary>
        /// Reserves the destination before the visual slide starts. Both the
        /// origin and destination stay occupied during transit, preventing two
        /// independently moving actors from sliding into the same square.
        /// </summary>
        public bool TryBeginStep(string actor, Vector2Int direction)
        {
            if (!positions.TryGetValue(actor, out Vector2Int origin)
                || destinations.ContainsKey(actor) || direction == Vector2Int.zero
                || Math.Abs(direction.x) > 1 || Math.Abs(direction.y) > 1)
                return false;

            Vector2Int target = origin + direction;
            if (!CanCross(origin,direction) || IsOccupied(target)) return false;

            destinations.Add(actor, target);
            return true;
        }

        /// <summary>Interrupt a slide without changing its occupied origin cell.</summary>
        public void CancelStep(string actor) => destinations.Remove(actor);

        public void CompleteStep(string actor)
        {
            if (!destinations.TryGetValue(actor, out Vector2Int target))
                throw new InvalidOperationException("Actor has no reserved step.");
            positions[actor] = target;
            destinations.Remove(actor);
        }

        public void RemoveActor(string actor)
        {
            destinations.Remove(actor);
            positions.Remove(actor);
        }
    }
}
