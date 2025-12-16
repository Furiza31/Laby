using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth
{
    public partial class Labyrinth
    {
        private class LabyrinthCrawler(int x, int y, Tile[,] tiles) : ICrawler
        {
            public int X => _x;

            public int Y => _y;

            public Task<Tile> GetFacingTileAsync()
            {
                var (_, _, tile) = GetFacingTile();

                return Task.FromResult(tile);
            }

            Direction ICrawler.Direction => _direction;

            public async Task<WalkResult> TryWalkAsync(Inventory crawlerInventory)
            {
                var (facingX, facingY, tile) = GetFacingTile();

                if (tile is Wall || tile is Outside)
                {
                    return new WalkResult(false, null);
                }

                if (tile is Door door && door.IsLocked)
                {
                    var opened = await door.OpenAsync(crawlerInventory);

                    if (!opened)
                    {
                        return new WalkResult(false, null);
                    }
                }

                if (!tile.IsTraversable)
                {
                    return new WalkResult(false, null);
                }

                var inventory = tile.Pass();

                _x = facingX;
                _y = facingY;
                return new WalkResult(true, inventory);
            }

            private bool IsOut(int pos, int dimension) =>
                pos < 0 || pos >= _tiles.GetLength(dimension);

            private (int X, int Y, Tile Tile) GetFacingTile()
            {
                int facingX = _x + _direction.DeltaX,
                    facingY = _y + _direction.DeltaY;

                var tile =
                    IsOut(facingX, dimension: 0) ||
                    IsOut(facingY, dimension: 1)
                        ? Outside.Singleton
                        : _tiles[facingX, facingY];

                return (facingX, facingY, tile);
            }

            private int _x = x;
            private int _y = y;

            private readonly Direction _direction = Direction.North;
            private readonly Tile[,] _tiles = tiles;
        }
    }
}
