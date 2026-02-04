using System.Collections.Concurrent;
using Laby.Core.Mapping;
using Laby.Core.Tiles;

namespace Laby.Mapping
{
    /// <summary>
    /// Thread-safe shared map of labyrinth observations from one or many explorers.
    /// </summary>
    public class SharedLabyrinthMap : ILabyrinthMap
    {
        public Type GetTileType(int x, int y) =>
            _observedTiles.TryGetValue(new MapPosition(x, y), out var tileType)
                ? tileType
                : typeof(Unknown);

        public bool TryGetTileType(int x, int y, out Type tileType) =>
            _observedTiles.TryGetValue(new MapPosition(x, y), out tileType!);

        public IReadOnlyDictionary<MapPosition, Type> Snapshot() =>
            new Dictionary<MapPosition, Type>(_observedTiles);

        public bool IsDoorKnownOpen(int x, int y) =>
            _openedDoors.ContainsKey(new MapPosition(x, y));

        public void Observe(int x, int y, Type tileType)
        {
            ArgumentNullException.ThrowIfNull(tileType);
            if (!typeof(Tile).IsAssignableFrom(tileType))
            {
                throw new ArgumentException(
                    $"Only tile types can be observed, got '{tileType.FullName}'.",
                    nameof(tileType)
                );
            }

            var position = new MapPosition(x, y);
            while (true)
            {
                if (_observedTiles.TryGetValue(position, out var existingType))
                {
                    if (existingType == tileType || tileType == typeof(Unknown))
                    {
                        return;
                    }

                    if (existingType == typeof(Unknown))
                    {
                        if (_observedTiles.TryUpdate(position, tileType, existingType))
                        {
                            return;
                        }
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"Conflicting observations at ({x},{y}): '{existingType.Name}' vs '{tileType.Name}'."
                    );
                }

                if (_observedTiles.TryAdd(position, tileType))
                {
                    return;
                }
            }
        }

        public void MarkDoorOpened(int x, int y)
        {
            var position = new MapPosition(x, y);
            _openedDoors[position] = true;
            Observe(x, y, typeof(Door));
        }

        private readonly ConcurrentDictionary<MapPosition, Type> _observedTiles = new();
        private readonly ConcurrentDictionary<MapPosition, bool> _openedDoors = new();
    }
}
