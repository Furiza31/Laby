using Laby.Core.Crawl;
using Laby.Core.Items;
using Laby.Core.Mapping;
using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    public class Explorer
    {
        public Explorer(ICrawler crawler, IExplorerStrategy strategy, ILabyrinthMap sharedMap)
        {
            ArgumentNullException.ThrowIfNull(crawler);
            ArgumentNullException.ThrowIfNull(strategy);
            ArgumentNullException.ThrowIfNull(sharedMap);

            _crawler = crawler;
            _strategy = strategy;
            _sharedMap = sharedMap;
            _explorerId = Interlocked.Increment(ref _nextExplorerId);
            
            _sharedMap.Observe(_crawler.X, _crawler.Y, typeof(Room));
        }

        public ICrawler Crawler => _crawler;
        public ILabyrinthMapReader SharedMap => _sharedMap;

        public async Task<int> GetOut(int n, Inventory? bag = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(n, 0, "n must be strictly positive");

            bag ??= new MyInventory();
            for (; n > 0; n--)
            {
                var facingX = _crawler.X + _crawler.Direction.DeltaX;
                var facingY = _crawler.Y + _crawler.Direction.DeltaY;
                var facingTileType = await _crawler.FacingTileType;
                _sharedMap.Observe(facingX, facingY, facingTileType);

                if (facingTileType == typeof(Outside))
                {
                    break;
                }

                var action = _strategy.NextAction(new ExplorerContext(
                    _crawler,
                    facingTileType,
                    bag,
                    _sharedMap,
                    _memory,
                    _explorerId));
                EventHandler<CrawlingEventArgs>? changeEvent;

                if (action == ExplorerAction.Walk
                    && facingTileType != typeof(Wall)
                    && await _crawler.TryWalk(bag) is Inventory roomContent)
                {
                    if (facingTileType != typeof(Door))
                    {
                        await bag.TryMoveItemsFrom(
                            roomContent,
                            roomContent.ItemTypes.Select(_ => true).ToList()
                        );
                    }
                    else
                    {
                        _sharedMap.MarkDoorOpened(_crawler.X, _crawler.Y);
                        _memory.MarkDoorOpened(new MapPosition(_crawler.X, _crawler.Y));
                    }
                    _sharedMap.Observe(_crawler.X, _crawler.Y, facingTileType);
                    changeEvent = PositionChanged;
                }
                else
                {
                    if (action == ExplorerAction.Walk && facingTileType == typeof(Door))
                    {
                        _memory.MarkDoorBlocked(new MapPosition(facingX, facingY), bag);
                    }

                    _crawler.Direction.TurnLeft();
                    changeEvent = DirectionChanged;
                }

                changeEvent?.Invoke(this, new CrawlingEventArgs(_crawler));
            }

            return n;
        }

        public event EventHandler<CrawlingEventArgs>? PositionChanged;

        public event EventHandler<CrawlingEventArgs>? DirectionChanged;

        private readonly ICrawler _crawler;
        private readonly IExplorerStrategy _strategy;
        private readonly ILabyrinthMap _sharedMap;
        private readonly ExplorerMemory _memory = new();
        private readonly long _explorerId;
        private static long _nextExplorerId;
    }
}
