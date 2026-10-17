using System.Collections;
using UnityEngine;

namespace Godlings
{
    /// <summary>
    /// Slides actors between cell centers using the shared angled projection.
    /// Visual facing is handled separately, so movement never changes aiming.
    /// </summary>
    public sealed class GridActor : MonoBehaviour
    {
        private GridBoard board;
        private string actorId;
        private float stepSeconds;
        private float cellSize;
        public bool IsMoving { get; private set; }
        public bool IsRecoiling { get; private set; }
        public Vector2Int Cell => board.PositionOf(actorId);
        public bool Occupies(Vector2Int cell) => board != null && board.ActorOccupies(actorId,cell);

        public void Initialize(GridBoard grid, string identity, Vector2Int start,
            float durationSeconds, float worldUnitsPerCell)
        {
            if (durationSeconds <= 0 || worldUnitsPerCell <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(durationSeconds));
            board = grid;
            actorId = identity;
            stepSeconds = durationSeconds;
            cellSize = worldUnitsPerCell;
            board.AddActor(actorId, start);
            transform.position = WorldPosition(start);
        }

        /// <summary>Changes the next step; a slide already underway keeps its duration.</summary>
        public void SetStepDuration(float seconds) => stepSeconds = Mathf.Clamp(seconds, 0.03f, 2f);

        public bool TryMove(Vector2Int direction)
        {
            if (board == null || IsMoving || !board.TryBeginStep(actorId, direction)) return false;
            StartCoroutine(Slide(board.PositionOf(actorId) + direction,stepSeconds,true));
            return true;
        }

        private Vector3 WorldPosition(Vector2Int cell) => GridProjection.World(cell) * cellSize;

        /// <summary>Knock back one legal tile over the given time; blocked recoil staggers in place.</summary>
        public void Recoil(Vector2Int direction, float seconds)
        {
            if (board == null) return;
            StopAllCoroutines();
            board.CancelStep(actorId);
            Vector2Int origin = board.PositionOf(actorId);
            bool reserved = board.TryBeginStep(actorId,direction);
            IsRecoiling = true;
            StartCoroutine(Slide(reserved ? origin+direction : origin,seconds,reserved));
        }

        private IEnumerator Slide(Vector2Int destination, float duration, bool completesReservation)
        {
            IsMoving = true;
            Vector3 start = transform.position, target = WorldPosition(destination);
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            transform.position = target;
            if (completesReservation) board.CompleteStep(actorId);
            IsRecoiling = false;
            IsMoving = false;
        }

        private void OnDestroy()
        {
            RemoveFromGrid();
        }

        public void RemoveFromGrid()
        {
            StopAllCoroutines();
            if (board != null) board.RemoveActor(actorId);
            board = null;
            IsRecoiling = false;
            IsMoving = false;
        }
    }
}
