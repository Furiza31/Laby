using Labyrinth.Crawl;

namespace Labyrinth.Navigation
{
    public interface IExplorator
    {
        /// <summary>
        /// Attempts to get out of the labyrinth in at most n moves.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        int GetOut(int n);

        /// <summary>
        /// Event fired when the position of the crawler changes.
        /// </summary>
        event EventHandler<CrawlingEventArgs>? PositionChanged;

        /// <summary>
        /// Event fired when the direction of the crawler changes.
        /// </summary>
        event EventHandler<CrawlingEventArgs>? DirectionChanged;
    }
}
