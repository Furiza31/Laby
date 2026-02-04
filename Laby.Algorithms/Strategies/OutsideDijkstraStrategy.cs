using Laby.Core.Mapping;
using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    /// <summary>
    /// Reaches the closest position that faces an already observed outside tile.
    /// </summary>
    public class OutsideDijkstraStrategy(int rotationOffset = 0) : DijkstraStrategyBase(rotationOffset)
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
                tileType => tileType == typeof(Outside),
                RotationOffset,
                out direction
            );
        }
    }
}
