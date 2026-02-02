using Laby.Core.Crawl;
using Laby.Core.Items;
using Laby.Core.Mapping;

namespace Laby.Algorithms
{
    public interface IExplorerStrategy
    {
        ExplorerAction NextAction(ExplorerContext context);
    }

    public readonly struct ExplorerContext
    {
        public ExplorerContext(ICrawler crawler, Type facingTileType, Inventory bag)
            : this(crawler, facingTileType, bag, map: null)
        {
        }

        public ExplorerContext(ICrawler crawler, Type facingTileType, Inventory bag, ILabyrinthMapReader? map)
        {
            Crawler = crawler;
            FacingTileType = facingTileType;
            Bag = bag;
            Map = map;
        }

        public ICrawler Crawler { get; }

        public Type FacingTileType { get; }

        public Inventory Bag { get; }

        public ILabyrinthMapReader? Map { get; }
    }
}
