namespace Labyrinth.Navigation
{
    public interface IExplorator
    {
        /// <summary>
        /// Try to get out of the labyrinth in at most the given number of steps.
        /// </summary>
        /// <param name="n">Maximum number of steps to try.</param>
        /// <returns>Remaining steps if we exited early, otherwise 0.</returns>
        Task<int> GetOutAsync(int n);

        event EventHandler<Crawl.CrawlingEventArgs> PositionChanged;

        event EventHandler<Crawl.CrawlingEventArgs> DirectionChanged;
    }
}
