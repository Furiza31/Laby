using Labyrinth.Items;

namespace Labyrinth.Crawl
{
    /// <summary>
    /// Labyrinth crawler interface.
    /// </summary>
    public interface ICrawler
    {
        /// <summary>
        /// Gets the current X position.
        /// </summary>
        int X { get; }

        /// <summary>
        /// Gets the current Y position.
        /// </summary>
        int Y { get; }

        /// <summary>
        /// Gets the current direction.
        /// </summary>
        Direction Direction { get; }

        /// <summary>
        /// Gets the type of the tile in front of the crawler.
        /// </summary>
        Task<Type> GetFacingTileTypeAsync();

        /// <summary>
        /// Pass the tile in front of the crawler and move into it.
        /// </summary>
        /// <param name="crawlerInventory">Inventory available to the crawler to interact with tiles.</param>
        /// <returns>Result of the move including collected inventory on success.</returns>
        Task<WalkResult> TryWalkAsync(Inventory crawlerInventory);
    }
}
