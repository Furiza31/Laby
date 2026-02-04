using Laby.Core.Items;
using Laby.Core.Mapping;
using Laby.Core.Tiles;
using System.Runtime.CompilerServices;

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
                _pendingDoor = null;
                return false;
            }

            var map = context.Map;
            var current = new MapPosition(context.Crawler.X, context.Crawler.Y);
            var keySignature = GetKeySignature(context.Bag);

            if (map.GetTileType(current.X, current.Y) == typeof(Door))
            {
                _crossedDoors.Add(current);
            }

            ResolvePendingAttempt(current, keySignature);

            if (TryAdjacentDoorDirection(map, current, keySignature, out var adjacentDirection, out var adjacentDoor))
            {
                _pendingDoor = adjacentDoor;
                _pendingKeySignature = keySignature;
                action = MapPathing.ActionToward(context.Crawler, adjacentDirection.Dx, adjacentDirection.Dy);
                return true;
            }

            if (!MapPathing.TryFindShortestPath(
                    map,
                    current,
                    p => p != current
                         && map.GetTileType(p.X, p.Y) == typeof(Door)
                         && !_crossedDoors.Contains(p)
                         && !IsDoorBlockedForCurrentKeys(p, keySignature),
                    rotationOffset,
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
            ILabyrinthMapReader map,
            MapPosition current,
            int keySignature,
            out (int Dx, int Dy) direction,
            out MapPosition doorPosition)
        {
            foreach (var candidate in MapPathing.OrderedDirections(rotationOffset))
            {
                var next = new MapPosition(current.X + candidate.Dx, current.Y + candidate.Dy);
                if (map.GetTileType(next.X, next.Y) != typeof(Door))
                {
                    continue;
                }

                if (_crossedDoors.Contains(next) || IsDoorBlockedForCurrentKeys(next, keySignature))
                {
                    continue;
                }

                direction = candidate;
                doorPosition = next;
                return true;
            }

            direction = default;
            doorPosition = default;
            return false;
        }

        private void ResolvePendingAttempt(MapPosition current, int keySignature)
        {
            if (_pendingDoor is not MapPosition pendingDoor)
            {
                return;
            }

            if (current == pendingDoor)
            {
                _crossedDoors.Add(pendingDoor);
                _blockedDoors.Remove(pendingDoor);
            }
            else if (_pendingKeySignature == keySignature)
            {
                _blockedDoors[pendingDoor] = keySignature;
            }

            _pendingDoor = null;
        }

        private bool IsDoorBlockedForCurrentKeys(MapPosition doorPosition, int keySignature) =>
            _blockedDoors.TryGetValue(doorPosition, out var blockedSignature)
            && blockedSignature == keySignature;

        private static int GetKeySignature(Inventory bag)
        {
            var hash = new HashCode();
            hash.Add(bag.ItemTypes.Count());

            if (bag is MyInventory localBag)
            {
                foreach (var item in localBag.Items)
                {
                    hash.Add(RuntimeHelpers.GetHashCode(item));
                }
            }

            return hash.ToHashCode();
        }

        private readonly Dictionary<MapPosition, int> _blockedDoors = new();
        private readonly HashSet<MapPosition> _crossedDoors = new();
        private MapPosition? _pendingDoor;
        private int _pendingKeySignature;
    }
}
