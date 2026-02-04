using Laby.Core.Mapping;
using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    /// <summary>
    /// Reaches the closest position that faces an already observed outside tile.
    /// </summary>
    public class OutsideDijkstraStrategy(int rotationOffset = 0) : IConditionalExplorerStrategy
    {
        public ExplorerAction NextAction(ExplorerContext context) =>
            TryNextAction(context, out var action)
                ? action
                : ExplorerAction.TurnLeft;

        public bool TryNextAction(ExplorerContext context, out ExplorerAction action)
        {
            action = default;

            if (context.Map is null)
            {
                return false;
            }

            var map = context.Map;
            var current = new MapPosition(context.Crawler.X, context.Crawler.Y);

            if (TryDirectionToOutside(map, current, out var outsideDirection))
            {
                action = MapPathing.ActionToward(context.Crawler, outsideDirection.Dx, outsideDirection.Dy);
                return true;
            }

            if (!MapPathing.TryFindShortestPath(
                    map,
                    current,
                    p => TryDirectionToOutside(map, p, out _),
                    rotationOffset,
                    out var path))
            {
                return false;
            }

            if (path.Count > 1)
            {
                var next = path[1];
                action = MapPathing.ActionToward(context.Crawler, next.X - current.X, next.Y - current.Y);
                return true;
            }

            if (TryDirectionToOutside(map, current, out outsideDirection))
            {
                action = MapPathing.ActionToward(context.Crawler, outsideDirection.Dx, outsideDirection.Dy);
                return true;
            }

            return false;
        }

        private bool TryDirectionToOutside(ILabyrinthMapReader map, MapPosition position, out (int Dx, int Dy) direction)
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
                rotationOffset,
                out direction
            );
        }
    }
}
