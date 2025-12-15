using Labyrinth.Sys;

namespace Labyrinth.Navigation
{
    public class RandomMovementStrategy(IEnumRandomizer<MoveAction> randomizer) : IMovementStrategy
    {
        private readonly IEnumRandomizer<MoveAction> _randomizer = randomizer ?? throw new ArgumentNullException(nameof(randomizer));

        public Task<MoveAction> NextActionAsync(Crawl.ICrawler crawler, Items.Inventory bag) =>
            Task.FromResult(_randomizer.Next());
    }
}
