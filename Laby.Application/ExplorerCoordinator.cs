using Laby.Algorithms;
using Laby.Core.Crawl;
using Laby.Core.Items;
using Laby.Core.Mapping;
using Laby.Algorithms.Sys;
using Laby.Mapping;

namespace Laby.Application
{
    public class ExplorerCoordinator
    {
        public ExplorerCoordinator(ICrawler crawler)
            : this(
                crawler,
                new RandExplorerStrategy(new BasicEnumRandomizer<ExplorerAction>()),
                new SharedLabyrinthMap()
            )
        {
        }

        public ExplorerCoordinator(ICrawler crawler, ILabyrinthMap sharedMap)
            : this(
                crawler,
                new RandExplorerStrategy(new BasicEnumRandomizer<ExplorerAction>()),
                sharedMap
            )
        {
        }

        public ExplorerCoordinator(ICrawler crawler, IExplorerStrategy strategy)
            : this(crawler, strategy, new SharedLabyrinthMap())
        {
        }

        public ExplorerCoordinator(ICrawler crawler, IExplorerStrategy strategy, ILabyrinthMap sharedMap)
        {
            _explorer = new Explorer(crawler, strategy, sharedMap);
            _explorer.PositionChanged += (_, e) => PositionChanged?.Invoke(this, e);
            _explorer.DirectionChanged += (_, e) => DirectionChanged?.Invoke(this, e);
        }

        public ICrawler Crawler => _explorer.Crawler;
        public ILabyrinthMapReader SharedMap => _explorer.SharedMap;

        public Task<int> GetOut(int n, Inventory? bag = null) => _explorer.GetOut(n, bag);

        public event EventHandler<CrawlingEventArgs>? PositionChanged;

        public event EventHandler<CrawlingEventArgs>? DirectionChanged;

        private readonly Explorer _explorer;
    }
}
