using Laby.Core.Mapping;
using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    /// <summary>
    /// Reaches the closest frontier (known traversable tile adjacent to an unknown tile).
    /// </summary>
    public class FrontierDijkstraStrategy(int rotationOffset = 0) : IConditionalExplorerStrategy
    {
        private readonly int _rotationOffset = rotationOffset;

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
            if (!MapPathing.IsNavigable(map.GetTileType(current.X, current.Y)))
            {
                FrontierClaimRegistry.Release(map, context.ExplorerId);
                return false;
            }

            if (TryDirectionToUnknown(map, current, out var currentDirection)
                && FrontierClaimRegistry.TryClaim(map, context.ExplorerId, current))
            {
                action = MapPathing.ActionToward(context.Crawler, currentDirection.Dx, currentDirection.Dy);
                return true;
            }

            var hasAnyPath = TryPathToFrontier(context, map, current, requireUnclaimed: false, out var anyPath);
            var hasUnclaimedPath = TryPathToFrontier(context, map, current, requireUnclaimed: true, out var unclaimedPath);

            if (hasUnclaimedPath
                && hasAnyPath
                && unclaimedPath.Count <= anyPath.Count + MaxDetourToAvoidOverlap
                && unclaimedPath.Count > 1
                && FrontierClaimRegistry.TryClaim(map, context.ExplorerId, unclaimedPath[^1]))
            {
                var next = unclaimedPath[1];
                action = MapPathing.ActionToward(context.Crawler, next.X - current.X, next.Y - current.Y);
                return true;
            }

            if (hasAnyPath && anyPath.Count > 1)
            {
                FrontierClaimRegistry.Release(map, context.ExplorerId);
                var next = anyPath[1];
                action = MapPathing.ActionToward(context.Crawler, next.X - current.X, next.Y - current.Y);
                return true;
            }

            if (hasUnclaimedPath
                && unclaimedPath.Count > 1
                && FrontierClaimRegistry.TryClaim(map, context.ExplorerId, unclaimedPath[^1]))
            {
                var next = unclaimedPath[1];
                action = MapPathing.ActionToward(context.Crawler, next.X - current.X, next.Y - current.Y);
                return true;
            }

            if (TryDirectionToUnknown(map, current, out currentDirection))
            {
                FrontierClaimRegistry.Release(map, context.ExplorerId);
                action = MapPathing.ActionToward(context.Crawler, currentDirection.Dx, currentDirection.Dy);
                return true;
            }

            FrontierClaimRegistry.Release(map, context.ExplorerId);
            return false;
        }

        private bool TryPathToFrontier(
            ExplorerContext context,
            ILabyrinthMapReader map,
            MapPosition current,
            bool requireUnclaimed,
            out IReadOnlyList<MapPosition> path) =>
            MapPathing.TryFindShortestPath(
                map,
                current,
                position => IsFrontier(context, map, position, requireUnclaimed),
                _rotationOffset,
                (position, tileType) => DoorTraversalPolicy.CanEnter(context, position, tileType),
                out path);

        private bool IsFrontier(
            ExplorerContext context,
            ILabyrinthMapReader map,
            MapPosition position,
            bool requireUnclaimed)
        {
            if (!MapPathing.IsNavigable(map.GetTileType(position.X, position.Y)))
            {
                return false;
            }

            if (!TryDirectionToUnknown(map, position, out _))
            {
                return false;
            }

            return !requireUnclaimed
                   || !FrontierClaimRegistry.IsClaimedByOther(map, context.ExplorerId, position);
        }

        private bool TryDirectionToUnknown(
            ILabyrinthMapReader map,
            MapPosition position,
            out (int Dx, int Dy) direction) =>
            MapPathing.TryFindAdjacentDirection(
                map,
                position,
                tileType => tileType == typeof(Unknown),
                _rotationOffset,
                out direction);

        private const int MaxDetourToAvoidOverlap = 1;
    }
}
