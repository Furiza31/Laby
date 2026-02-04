using Laby.Core.Tiles;

namespace Laby.Algorithms
{
    /// <summary>
    /// Simple deterministic fallback strategy.
    /// </summary>
    public class LeftWallFollowerStrategy : IExplorerStrategy
    {
        public ExplorerAction NextAction(ExplorerContext context)
        {
            if (context.FacingTileType == typeof(Wall))
            {
                return ExplorerAction.TurnLeft;
            }

            if (context.Map is null)
            {
                return ExplorerAction.Walk;
            }

            var crawler = context.Crawler;
            var leftX = crawler.X + crawler.Direction.DeltaY;
            var leftY = crawler.Y - crawler.Direction.DeltaX;
            var leftTile = context.Map.GetTileType(leftX, leftY);

            if (leftTile != typeof(Wall) && leftTile != typeof(Outside))
            {
                return ExplorerAction.TurnLeft;
            }

            return ExplorerAction.Walk;
        }
    }
}
