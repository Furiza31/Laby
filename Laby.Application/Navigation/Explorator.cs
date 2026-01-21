using Laby.Algorithms.Navigation;
using Laby.Core.Crawl;
using Laby.Core.Items;
using Laby.Core.Tiles;

namespace Laby.Application.Navigation
{
    public class Explorator(ICrawler crawler, IMovementStrategy strategy) : IExplorator
    {
        private readonly ICrawler _crawler = crawler;
        private readonly IMovementStrategy _strategy = strategy;

        public int GetOut(int n)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(n, 0, "n must be strictly positive");
            MyInventory bag = new();

            for (; n > 0 && _crawler.FacingTile is not Outside; n--)
            {
                var changeEvent = _crawler.FacingTile.IsTraversable
                                   ? ExecuteAction(_strategy.NextMove(_crawler), bag)
                                   : TurnLeft();

                TryOpenLockedDoor(bag);
                RaiseChangeEvent(changeEvent);
            }

            return n;
        }

        private EventHandler<CrawlingEventArgs>? ExecuteAction(MoveAction action, MyInventory bag) =>
            action switch
            {
                MoveAction.None => null,
                MoveAction.Walk => WalkAndCollect(bag),
                MoveAction.TurnRight => TurnRight(),
                MoveAction.TurnLeft => TurnLeft(),
                _ => throw new InvalidOperationException("Invalid move action")
            };

        private EventHandler<CrawlingEventArgs>? WalkAndCollect(MyInventory bag)
        {
            var roomContent = _crawler.Walk();

            while (roomContent.HasItems)
            {
                bag.MoveItemFrom(roomContent);
            }

            return PositionChanged;
        }

        private EventHandler<CrawlingEventArgs>? TurnLeft()
        {
            _crawler.Direction.TurnLeft();
            return DirectionChanged;
        }

        private EventHandler<CrawlingEventArgs>? TurnRight()
        {
            _crawler.Direction.TurnRight();
            return DirectionChanged;
        }

        private void TryOpenLockedDoor(MyInventory bag)
        {
            if (_crawler.FacingTile is not Door { IsLocked: true } door) return;
            var attempts = bag.Items.Count();
            for (var i = 0; i < attempts && door.IsLocked; i++)
            {
                _ = door.Open(bag);
            }
        }

        private void RaiseChangeEvent(EventHandler<CrawlingEventArgs>? changeEvent)
        {
            changeEvent?.Invoke(this, new CrawlingEventArgs(_crawler));
        }

        public event EventHandler<CrawlingEventArgs>? PositionChanged;

        public event EventHandler<CrawlingEventArgs>? DirectionChanged;
    }
}
