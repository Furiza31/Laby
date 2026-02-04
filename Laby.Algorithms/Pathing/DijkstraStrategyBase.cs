using Laby.Core.Mapping;

namespace Laby.Algorithms
{
    /// <summary>
    /// Shared pathfinding workflow used by Dijkstra-based conditional strategies.
    /// </summary>
    public abstract class DijkstraStrategyBase(int rotationOffset) : IConditionalExplorerStrategy
    {
        private readonly int _rotationOffset = rotationOffset;

        protected int RotationOffset => _rotationOffset;

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

            if (TryDirectionToGoal(context, map, current, out var goalDirection))
            {
                action = MapPathing.ActionToward(context.Crawler, goalDirection.Dx, goalDirection.Dy);
                return true;
            }

            if (!MapPathing.TryFindShortestPath(
                    map,
                    current,
                    position => TryDirectionToGoal(context, map, position, out _),
                    _rotationOffset,
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

            if (TryDirectionToGoal(context, map, current, out goalDirection))
            {
                action = MapPathing.ActionToward(context.Crawler, goalDirection.Dx, goalDirection.Dy);
                return true;
            }

            return false;
        }

        protected virtual bool CanEnter(ExplorerContext context, MapPosition position, Type tileType) =>
            DoorTraversalPolicy.CanEnter(context, position, tileType);

        protected abstract bool TryDirectionToGoal(
            ExplorerContext context,
            ILabyrinthMapReader map,
            MapPosition position,
            out (int Dx, int Dy) direction);
    }
}
