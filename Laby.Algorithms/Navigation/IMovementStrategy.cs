using Laby.Core.Crawl;

namespace Laby.Algorithms.Navigation
{
    public interface IMovementStrategy
    {
        /// <summary>
        /// Decides the next move for the crawler.
        /// </summary>
        /// <param name="crawler">The crawler for which to decide the next move.</param>
        /// <returns>The next move action.</returns>
        MoveAction NextMove(ICrawler crawler);
    }
}
