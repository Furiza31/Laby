using Laby.Algorithms.Sys;
using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    public class RandExplorerStrategy(IEnumRandomizer<ExplorerAction> rnd) : IExplorerStrategy
    {
        private readonly IEnumRandomizer<ExplorerAction> _rnd = rnd;

        public ExplorerAction NextAction(ExplorerContext context)
        {
            if (context.FacingTileType == typeof(Wall))
            {
                return ExplorerAction.TurnLeft;
            }

            return _rnd.Next();
        }
    }
}
