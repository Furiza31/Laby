using Laby.Core.Mapping;
using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    /// <summary>
    /// Reaches the closest frontier (known traversable tile adjacent to an unknown tile).
    /// </summary>
    public class FrontierDijkstraStrategy(int rotationOffset = 0) : DijkstraStrategyBase(rotationOffset)
    {
        protected override bool TryDirectionToGoal(
            ExplorerContext context,
            ILabyrinthMapReader map,
            MapPosition position,
            out (int Dx, int Dy) direction)
        {
            if (!MapPathing.IsNavigable(map.GetTileType(position.X, position.Y)))
            {
                direction = default;
                return false;
            }

            return MapPathing.TryFindAdjacentDirection(
                map,
                position,
                tileType => tileType == typeof(Unknown),
                RotationOffset,
                out direction
            );
        }
    }
}
