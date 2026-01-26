using Labyrinth;
using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Sys;

namespace Labyrinth.Application
{
    public class ExplorerCoordinator
    {
        public ExplorerCoordinator(ICrawler crawler)
            : this(crawler, new BasicEnumRandomizer<RandExplorer.Actions>())
        {
        }

        public ExplorerCoordinator(ICrawler crawler, IEnumRandomizer<RandExplorer.Actions> randomizer)
        {
            _explorer = new RandExplorer(crawler, randomizer);
            _explorer.PositionChanged += (_, e) => PositionChanged?.Invoke(this, e);
            _explorer.DirectionChanged += (_, e) => DirectionChanged?.Invoke(this, e);
        }

        public ICrawler Crawler => _explorer.Crawler;

        public Task<int> GetOut(int n, Inventory? bag = null) => _explorer.GetOut(n, bag);

        public event EventHandler<CrawlingEventArgs>? PositionChanged;

        public event EventHandler<CrawlingEventArgs>? DirectionChanged;

        private readonly RandExplorer _explorer;
    }
}
