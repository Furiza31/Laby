using Laby.Core.Mapping;
using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    /// <summary>
    /// Reaches the closest frontier (known traversable tile adjacent to an unknown tile).
    /// </summary>
    public class FrontierDijkstraStrategy(int rotationOffset = 0) : IConditionalExplorerStrategy
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

            if (TryDirectionToUnknown(map, current, out var unknownDirection))
            {
                action = MapPathing.ActionToward(context.Crawler, unknownDirection.Dx, unknownDirection.Dy);
                return true;
            }

            if (!MapPathing.TryFindShortestPath(
                    map,
                    current,
                    p => TryDirectionToUnknown(map, p, out _),
                    rotationOffset,
                    (position, tileType) => CanEnter(context, position, tileType),
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

            if (TryDirectionToUnknown(map, current, out unknownDirection))
            {
                action = MapPathing.ActionToward(context.Crawler, unknownDirection.Dx, unknownDirection.Dy);
                return true;
            }

            return false;
        }

        private bool TryDirectionToUnknown(ILabyrinthMapReader map, MapPosition position, out (int Dx, int Dy) direction)
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
                rotationOffset,
                out direction
            );
        }

        private static bool CanEnter(ExplorerContext context, MapPosition position, Type tileType)
        {
            if (tileType != typeof(Door))
            {
                return true;
            }

            if (context.Map is not null && context.Map.IsDoorKnownOpen(position.X, position.Y))
            {
                return true;
            }

            return context.Memory is null || !context.Memory.IsDoorBlocked(position, context.Bag);
        }
    }
}
