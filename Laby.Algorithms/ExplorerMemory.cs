using System.Runtime.CompilerServices;
using Laby.Core.Items;
using Laby.Core.Mapping;

namespace Laby.Algorithms
{
    /// <summary>
    /// Per-explorer memory used to avoid retrying known impossible doors.
    /// </summary>
    public sealed class ExplorerMemory
    {
        public void MarkDoorOpened(MapPosition doorPosition)
        {
            _openedDoors.Add(doorPosition);
            _blockedDoorSignatures.Remove(doorPosition);
        }

        public bool IsDoorKnownOpen(MapPosition doorPosition) =>
            _openedDoors.Contains(doorPosition);

        public void MarkDoorBlocked(MapPosition doorPosition, Inventory bag)
        {
            if (_openedDoors.Contains(doorPosition))
            {
                return;
            }

            var signature = GetKeySignature(bag);
            if (!_blockedDoorSignatures.TryGetValue(doorPosition, out var knownSignatures))
            {
                knownSignatures = new HashSet<int>();
                _blockedDoorSignatures[doorPosition] = knownSignatures;
            }
            knownSignatures.Add(signature);
        }

        public bool IsDoorBlocked(MapPosition doorPosition, Inventory bag) =>
            _blockedDoorSignatures.TryGetValue(doorPosition, out var knownSignatures)
            && knownSignatures.Contains(GetKeySignature(bag));

        public int GetKeySignature(Inventory bag)
        {
            var hash = new HashCode();
            hash.Add(bag.ItemTypes.Count());

            if (bag is MyInventory localBag)
            {
                foreach (var itemHash in localBag.Items
                             .Select(RuntimeHelpers.GetHashCode)
                             .OrderBy(value => value))
                {
                    hash.Add(itemHash);
                }
            }
            else
            {
                hash.Add(bag.ItemTypes.Count(type => type == typeof(Key)));
            }

            return hash.ToHashCode();
        }

        private readonly HashSet<MapPosition> _openedDoors = new();
        private readonly Dictionary<MapPosition, HashSet<int>> _blockedDoorSignatures = new();
    }
}
