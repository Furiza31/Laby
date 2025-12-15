using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Sys;
using Labyrinth.Tiles;

namespace Labyrinth
{
    public class RandExplorer(ICrawler crawler, IEnumRandomizer<RandExplorer.Actions> rnd)
    {
        private readonly ICrawler _crawler = crawler;
        private readonly IEnumRandomizer<Actions> _rnd = rnd;

        public enum Actions
        {
            TurnLeft,
            Walk
        }

        public async Task<int> GetOutAsync(int n)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(n, 0, "n must be strictly positive");
            MyInventory bag = new();

            while (n > 0)
            {
                var facingType = await _crawler.GetFacingTileTypeAsync();

                if (facingType == typeof(Outside))
                {
                    break;
                }

                EventHandler<CrawlingEventArgs>? changeEvent;

                var shouldWalk =
                    facingType != typeof(Wall) &&
                    facingType != typeof(Outside) &&
                    _rnd.Next() == Actions.Walk;

                if (shouldWalk)
                {
                    var walkResult = await _crawler.TryWalkAsync(bag);

                    if (walkResult.Success)
                    {
                        if (walkResult.Inventory is { } roomContent)
                        {
                            while (await bag.TryMoveItemFromAsync(roomContent))
                            {
                                ;
                            }
                        }
                        changeEvent = PositionChanged;
                    }
                    else
                    {
                        _crawler.Direction.TurnLeft();
                        changeEvent = DirectionChanged;
                    }

                }
                else
                {
                    _crawler.Direction.TurnLeft();
                    changeEvent = DirectionChanged;
                }

                n--;
                changeEvent?.Invoke(this, new CrawlingEventArgs(_crawler));
            }
            return n;
        }

        public event EventHandler<CrawlingEventArgs>? PositionChanged;

        public event EventHandler<CrawlingEventArgs>? DirectionChanged;
    }

}
