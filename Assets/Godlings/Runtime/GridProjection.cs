using UnityEngine;

namespace Godlings
{
    /// <summary>
    /// A simple angled floor beneath upright sprites. This is test geometry,
    /// independent of the final tile artwork's pixel dimensions.
    /// </summary>
    public static class GridProjection
    {
        public static Vector3 World(Vector2 cell) => new(cell.x - cell.y * 0.45f, cell.y * 0.55f, 0);
        public static Vector2 Logical(Vector3 world)
        {
            float y = world.y/0.55f;
            return new Vector2(world.x+y*0.45f,y);
        }
        public static Vector3 World(Vector2Int cell) => World((Vector2)cell);
    }
}
