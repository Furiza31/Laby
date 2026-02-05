namespace Laby.Core.Mapping
{
    /// <summary>
    /// Read-only access to a shared map of observed labyrinth tiles.
    /// </summary>
    public interface ILabyrinthMapReader
    {
        /// <summary>
        /// Gets the observed tile type at a position, or <see cref="Tiles.Unknown"/> if unseen.
        /// </summary>
        Type GetTileType(int x, int y);

        /// <summary>
        /// Tries to get an observed tile type at a position.
        /// </summary>
        bool TryGetTileType(int x, int y, out Type tileType);

        /// <summary>
        /// Returns a snapshot of all observed tiles.
        /// </summary>
        IReadOnlyDictionary<MapPosition, Type> Snapshot();

        /// <summary>
        /// True when a door at the given position is known to be opened by any explorer.
        /// </summary>
        bool IsDoorKnownOpen(int x, int y);
    }

    /// <summary>
    /// Mutable shared map used by explorers to merge observations.
    /// </summary>
    public interface ILabyrinthMap : ILabyrinthMapReader
    {
        /// <summary>
        /// Stores a tile type observation at the provided coordinates.
        /// </summary>
        void Observe(int x, int y, Type tileType);

        /// <summary>
        /// Marks a door as opened at the provided coordinates.
        /// </summary>
        void MarkDoorOpened(int x, int y);
    }
}
