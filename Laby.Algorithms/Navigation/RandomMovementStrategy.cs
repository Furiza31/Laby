using Laby.Core.Crawl;
using Laby.Algorithms.Sys;

namespace Laby.Algorithms.Navigation
{
    public class RandomMovementStrategy(IEnumRandomizer<MoveAction> randomizer) : IMovementStrategy
    {
        private readonly IEnumRandomizer<MoveAction> _randomizer = randomizer;

        public MoveAction NextMove(ICrawler crawler)
        {
            MoveAction action;
            do
            {
                action = _randomizer.Next();
            } while (action == MoveAction.None);
            return action;
        }
    }
}
