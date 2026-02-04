using Laby.Core.Crawl;
using Laby.Core.Mapping;
using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    internal static class MapPathing
    {
        private static readonly (int Dx, int Dy)[] Directions =
        [
            (0, -1),
            (1, 0),
            (0, 1),
            (-1, 0)
        ];

        public static bool IsNavigable(Type tileType) =>
            tileType == typeof(Room) || tileType == typeof(Door);

        public static IEnumerable<(int Dx, int Dy)> OrderedDirections(int rotationOffset)
        {
            var offset = NormalizeRotation(rotationOffset);
            for (var i = 0; i < Directions.Length; i++)
            {
                yield return Directions[(i + offset) % Directions.Length];
            }
        }

        public static ExplorerAction ActionToward(ICrawler crawler, int deltaX, int deltaY) =>
            crawler.Direction.DeltaX == deltaX && crawler.Direction.DeltaY == deltaY
                ? ExplorerAction.Walk
                : ExplorerAction.TurnLeft;

        public static bool TryFindAdjacentDirection(
            ILabyrinthMapReader map,
            MapPosition origin,
            Func<Type, bool> predicate,
            int rotationOffset,
            out (int Dx, int Dy) direction)
        {
            foreach (var candidate in OrderedDirections(rotationOffset))
            {
                if (predicate(map.GetTileType(origin.X + candidate.Dx, origin.Y + candidate.Dy)))
                {
                    direction = candidate;
                    return true;
                }
            }

            direction = default;
            return false;
        }

        public static bool TryFindShortestPath(
            ILabyrinthMapReader map,
            MapPosition start,
            Func<MapPosition, bool> isGoal,
            int rotationOffset,
            out IReadOnlyList<MapPosition> path)
        {
            if (isGoal(start))
            {
                path = [start];
                return true;
            }

            var queue = new Queue<MapPosition>();
            var visited = new HashSet<MapPosition> { start };
            var previous = new Dictionary<MapPosition, MapPosition>();

            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var (dx, dy) in OrderedDirections(rotationOffset))
                {
                    var next = new MapPosition(current.X + dx, current.Y + dy);
                    if (!visited.Add(next))
                    {
                        continue;
                    }

                    if (!IsNavigable(map.GetTileType(next.X, next.Y)))
                    {
                        continue;
                    }

                    previous[next] = current;
                    if (isGoal(next))
                    {
                        path = BuildPath(start, next, previous);
                        return true;
                    }

                    queue.Enqueue(next);
                }
            }

            path = Array.Empty<MapPosition>();
            return false;
        }

        private static IReadOnlyList<MapPosition> BuildPath(
            MapPosition start,
            MapPosition goal,
            IReadOnlyDictionary<MapPosition, MapPosition> previous)
        {
            var path = new List<MapPosition> { goal };
            var current = goal;
            while (current != start)
            {
                current = previous[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }

        private static int NormalizeRotation(int rotationOffset)
        {
            var normalized = rotationOffset % Directions.Length;
            if (normalized < 0)
            {
                normalized += Directions.Length;
            }
            return normalized;
        }
    }
}
