using Laby.Core.Mapping;
using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    internal static class DoorTraversalPolicy
    {
        public static bool CanEnter(ExplorerContext context, MapPosition position, Type tileType)
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
