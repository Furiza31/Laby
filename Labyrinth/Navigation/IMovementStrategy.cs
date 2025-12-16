using Labyrinth.Crawl;
using Labyrinth.Items;

namespace Labyrinth.Navigation
{
    /// <summary>
    /// Strategy that decides the next move for an explorator.
    /// </summary>
    public interface IMovementStrategy
    {
        /// <summary>
        /// Computes the next move to perform.
        /// </summary>
        /// <param name="crawler">Current crawler state.</param>
        /// <param name="bag">Inventory currently held by the explorator.</param>
        Task<MoveAction> NextActionAsync(ICrawler crawler, Inventory bag);
    }
}
