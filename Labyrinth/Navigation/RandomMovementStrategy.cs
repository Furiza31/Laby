using Labyrinth.Crawl;
using Labyrinth.Sys;
using Labyrinth.Tiles;

namespace Labyrinth.Navigation
{
    public class RandomMovementStrategy(IEnumRandomizer<MoveAction> randomizer) : IMovementStrategy
    {
        private readonly IEnumRandomizer<MoveAction> _randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));

        public async Task<MoveAction> NextActionAsync(ICrawler crawler, Items.Inventory bag) =>
            await crawler.GetFacingTileAsync() is Wall ? MoveAction.TurnLeft : _randomizer.Next();
    }
}
