using Laby.Core.Items;
using Laby.Core.Tiles;

namespace Laby.Core.Build
{
    public interface IBuilder
    {
        Tile[,] Build();

        event EventHandler<StartEventArgs>? StartPositionFound;
    }
}
