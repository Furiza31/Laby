using Laby.Core.Crawl;
using Laby.Core.Items;
using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    public class Explorer(ICrawler crawler, IExplorerStrategy strategy)
    {
        private readonly ICrawler _crawler = crawler;
        private readonly IExplorerStrategy _strategy = strategy;

        public ICrawler Crawler => _crawler;

        public async Task<int> GetOut(int n, Inventory? bag = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(n, 0, "n must be strictly positive");

            bag ??= new MyInventory();
            for (; n > 0; n--)
            {
                var facingTileType = await _crawler.FacingTileType;

                if (facingTileType == typeof(Outside))
                {
                    break;
                }

                var action = _strategy.NextAction(new ExplorerContext(_crawler, facingTileType, bag));
                EventHandler<CrawlingEventArgs>? changeEvent;

                if (action == ExplorerAction.Walk
                    && facingTileType != typeof(Wall)
                    && await _crawler.TryWalk(bag) is Inventory roomContent)
                {
                    await bag.TryMoveItemsFrom(
                        roomContent,
                        roomContent.ItemTypes.Select(_ => true).ToList()
                    );
                    changeEvent = PositionChanged;
                }
                else
                {
                    _crawler.Direction.TurnLeft();
                    changeEvent = DirectionChanged;
                }

                changeEvent?.Invoke(this, new CrawlingEventArgs(_crawler));
            }

            return n;
        }

        public event EventHandler<CrawlingEventArgs>? PositionChanged;

        public event EventHandler<CrawlingEventArgs>? DirectionChanged;
    }
}
