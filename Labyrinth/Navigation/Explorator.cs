using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth.Navigation
{
    /// <summary>
    /// Explorator that delegates movement decisions to a strategy.
    /// </summary>
    public class Explorator(ICrawler crawler, IMovementStrategy strategy) : IExplorator
    {
        private readonly ICrawler _crawler = crawler ?? throw new ArgumentNullException(nameof(crawler));
        private readonly IMovementStrategy _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));

        public async Task<int> GetOutAsync(int n)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(n, 0, "n must be strictly positive");

            var bag = new MyInventory();

            for (
                var facingTile = await _crawler.GetFacingTileAsync();
                n > 0 && facingTile is not Outside;
                n--, facingTile = await _crawler.GetFacingTileAsync()
            )
            {
                EventHandler<CrawlingEventArgs>? changeEvent;

                if (facingTile is Wall)
                {
                    changeEvent = TurnAndNotify(_crawler.Direction.TurnLeft);
                }
                else
                {
                    var action = await _strategy.NextActionAsync(_crawler, bag);
                    changeEvent = action switch
                    {
                        MoveAction.TurnRight => TurnAndNotify(_crawler.Direction.TurnRight),
                        MoveAction.Walk => await TryWalkAsync(bag),
                        MoveAction.TurnLeft => TurnAndNotify(_crawler.Direction.TurnLeft),
                        _ => throw new InvalidOperationException("Unknown MoveAction")
                    };
                }
                if (facingTile is Door door && door.IsLocked)
                {
                    TryOpenDoor(door, bag);
                }
                changeEvent?.Invoke(this, new CrawlingEventArgs(_crawler));
            }

            return n;
        }

        private EventHandler<CrawlingEventArgs>? TurnAndNotify(Action turnAction)
        {
            turnAction();
            return DirectionChanged;
        }

        private async Task<EventHandler<CrawlingEventArgs>?> TryWalkAsync(MyInventory bag)
        {
            var walkResult = await _crawler.TryWalkAsync(bag);

            if (walkResult.Success)
            {
                if (walkResult.Inventory is Inventory roomContent)
                {
                    while (await bag.TryMoveItemFromAsync(roomContent)) ;
                }
                return PositionChanged;
            }

            _crawler.Direction.TurnLeft();
            return DirectionChanged;
        }

        private static async void TryOpenDoor(Door door, MyInventory bag)
        {
            while (bag.HasItems && !await door.OpenAsync(bag)) ;
        }

        public event EventHandler<CrawlingEventArgs>? PositionChanged;

        public event EventHandler<CrawlingEventArgs>? DirectionChanged;
    }
}
