using UnityEngine;

namespace Godlings
{
    public static class EnemyAwareness
    {
        /// <summary>
        /// Measures straight-line distance in logical tile coordinates, before
        /// the angled screen projection. No remembered pursuit survives outside
        /// this boundary. An already-started grid slide finishes at its center.
        /// </summary>
        public static bool CanSense(Vector2Int enemy, Vector2Int player, float radiusTiles)
        {
            return radiusTiles > 0 && ((Vector2)(enemy-player)).sqrMagnitude <= radiusTiles*radiusTiles;
        }
    }
}
