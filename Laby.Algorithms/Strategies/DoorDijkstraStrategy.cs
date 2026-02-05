using Laby.Core.Mapping;
using Laby.Core.Tiles;
using Laby.Core.Items;

namespace Laby.Algorithms
{
    /// <summary>
    /// When carrying at least one key, heads toward the nearest known door.
    /// </summary>
    public class DoorDijkstraStrategy(int rotationOffset = 0) : IConditionalExplorerStrategy
    {
        public ExplorerAction NextAction(ExplorerContext context) =>
            TryNextAction(context, out var action)
                ? action
                : ExplorerAction.TurnLeft;

        public bool TryNextAction(ExplorerContext context, out ExplorerAction action)
        {
            action = default;

            if (context.Map is null || !context.Bag.ItemTypes.Any(type => type == typeof(Key)))
            {
                return false;
            }

            var map = context.Map;
            var current = new MapPosition(context.Crawler.X, context.Crawler.Y);
            if (TryAdjacentDoorDirection(context, map, current, out var adjacentDirection))
            {
                action = MapPathing.ActionToward(context.Crawler, adjacentDirection.Dx, adjacentDirection.Dy);
                return true;
            }

            if (!MapPathing.TryFindShortestPath(
                    map,
                    current,
                    p => IsDoorTarget(context, map, current, p),
                    rotationOffset,
                    (position, tileType) => DoorTraversalPolicy.CanEnter(context, position, tileType),
                    out var path))
            {
                return false;
            }

            if (path.Count <= 1)
            {
                return false;
            }

            var next = path[1];
            action = MapPathing.ActionToward(context.Crawler, next.X - current.X, next.Y - current.Y);
            return true;
        }

        private bool TryAdjacentDoorDirection(
            ExplorerContext context,
            ILabyrinthMapReader map,
            MapPosition current,
            out (int Dx, int Dy) direction)
        {
            foreach (var candidate in MapPathing.OrderedDirections(rotationOffset))
            {
                var next = new MapPosition(current.X + candidate.Dx, current.Y + candidate.Dy);
                if (!IsDoorTarget(context, map, current, next))
                {
                    continue;
                }

                direction = candidate;
                return true;
            }

            direction = default;
            return false;
        }

        private static bool IsDoorTarget(
            ExplorerContext context,
            ILabyrinthMapReader map,
            MapPosition current,
            MapPosition position)
        {
            if (position == current || map.GetTileType(position.X, position.Y) != typeof(Door))
            {
                return false;
            }

            if (map.IsDoorKnownOpen(position.X, position.Y))
            {
                return false;
            }

            if (context.Memory is null)
            {
                return true;
            }

            return !context.Memory.IsDoorKnownOpen(position)
                   && !context.Memory.IsDoorBlocked(position, context.Bag);
        }
    }
}
