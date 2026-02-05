using System.Runtime.CompilerServices;
using Laby.Core.Mapping;
using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    /// <summary>
    /// Coordinates frontier targets between explorers that share the same map.
    /// </summary>
    internal static class FrontierClaimRegistry
    {
        public static bool IsClaimedByOther(
            ILabyrinthMapReader map,
            long explorerId,
            MapPosition frontier)
        {
            if (explorerId <= 0)
            {
                return false;
            }

            var state = _claimsByMap.GetValue(map, _ => new MapClaimState());
            lock (state.SyncRoot)
            {
                CleanupStaleClaims(map, state);
                return state.ExplorerByFrontier.TryGetValue(frontier, out var owner)
                    && owner != explorerId;
            }
        }

        public static bool TryClaim(
            ILabyrinthMapReader map,
            long explorerId,
            MapPosition frontier)
        {
            if (explorerId <= 0)
            {
                return true;
            }

            var state = _claimsByMap.GetValue(map, _ => new MapClaimState());
            lock (state.SyncRoot)
            {
                CleanupStaleClaims(map, state);

                if (state.ExplorerByFrontier.TryGetValue(frontier, out var owner)
                    && owner != explorerId)
                {
                    return false;
                }

                if (state.FrontierByExplorer.TryGetValue(explorerId, out var previousFrontier)
                    && previousFrontier != frontier)
                {
                    state.ExplorerByFrontier.Remove(previousFrontier);
                }

                state.ExplorerByFrontier[frontier] = explorerId;
                state.FrontierByExplorer[explorerId] = frontier;
                return true;
            }
        }

        public static void Release(ILabyrinthMapReader map, long explorerId)
        {
            if (explorerId <= 0)
            {
                return;
            }

            if (!_claimsByMap.TryGetValue(map, out var state))
            {
                return;
            }

            lock (state.SyncRoot)
            {
                if (!state.FrontierByExplorer.TryGetValue(explorerId, out var frontier))
                {
                    return;
                }

                state.FrontierByExplorer.Remove(explorerId);
                state.ExplorerByFrontier.Remove(frontier);
            }
        }

        private static void CleanupStaleClaims(ILabyrinthMapReader map, MapClaimState state)
        {
            foreach (var (frontier, owner) in state.ExplorerByFrontier.ToArray())
            {
                if (IsFrontier(map, frontier))
                {
                    continue;
                }

                state.ExplorerByFrontier.Remove(frontier);
                if (state.FrontierByExplorer.TryGetValue(owner, out var knownFrontier)
                    && knownFrontier == frontier)
                {
                    state.FrontierByExplorer.Remove(owner);
                }
            }
        }

        private static bool IsFrontier(ILabyrinthMapReader map, MapPosition position)
        {
            if (!MapPathing.IsNavigable(map.GetTileType(position.X, position.Y)))
            {
                return false;
            }

            return MapPathing.TryFindAdjacentDirection(
                map,
                position,
                tileType => tileType == typeof(Unknown),
                rotationOffset: 0,
                out _);
        }

        private static readonly ConditionalWeakTable<ILabyrinthMapReader, MapClaimState> _claimsByMap = new();

        private sealed class MapClaimState
        {
            public object SyncRoot { get; } = new();
            public Dictionary<MapPosition, long> ExplorerByFrontier { get; } = new();
            public Dictionary<long, MapPosition> FrontierByExplorer { get; } = new();
        }
    }
}
