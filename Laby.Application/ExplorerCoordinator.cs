using Laby.Algorithms;
using Laby.Core.Crawl;
using Laby.Core.Items;
using Laby.Algorithms.Sys;

namespace Laby.Application
{
    public class ExplorerCoordinator
    {
        public ExplorerCoordinator(ICrawler crawler)
            : this(crawler, new RandExplorerStrategy(new BasicEnumRandomizer<ExplorerAction>()))
        {
        }

        public ExplorerCoordinator(ICrawler crawler, IEnumRandomizer<ExplorerAction> randomizer)
            : this(crawler, new RandExplorerStrategy(randomizer))
        {
        }

        public ExplorerCoordinator(ICrawler crawler, IExplorerStrategy strategy)
        {
            _explorer = new Explorer(crawler, strategy);
            _explorer.PositionChanged += (_, e) => PositionChanged?.Invoke(this, e);
            _explorer.DirectionChanged += (_, e) => DirectionChanged?.Invoke(this, e);
        }

        public ICrawler Crawler => _explorer.Crawler;

        public Task<int> GetOut(int n, Inventory? bag = null) => _explorer.GetOut(n, bag);

        public event EventHandler<CrawlingEventArgs>? PositionChanged;

        public event EventHandler<CrawlingEventArgs>? DirectionChanged;

        private readonly Explorer _explorer;
    }
}
