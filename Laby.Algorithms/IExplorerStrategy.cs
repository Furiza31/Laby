using Labyrinth.Crawl;
using Labyrinth.Items;

namespace Labyrinth
{
    public interface IExplorerStrategy
    {
        ExplorerAction NextAction(ExplorerContext context);
    }

    public readonly struct ExplorerContext
    {
        public ExplorerContext(ICrawler crawler, Type facingTileType, Inventory bag)
        {
            Crawler = crawler;
            FacingTileType = facingTileType;
            Bag = bag;
        }

        public ICrawler Crawler { get; }

        public Type FacingTileType { get; }

        public Inventory Bag { get; }
    }
}
