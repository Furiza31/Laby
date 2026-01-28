using Laby.Application;
using Laby.Core;
using Laby.Core.Crawl;
using Laby.Core.Tiles;
using Laby.Infrastructure.ApiClient;
using SysConsole = System.Console;

namespace Laby.Client.Console;

internal sealed class ExplorerRenderer
{
    private const int OffsetY = 2;

    private readonly ExplorerCoordinator explorer;
    private readonly Dictionary<Type, char> tileToChar;
    private int prevX;
    private int prevY;

    public ExplorerRenderer(ExplorerCoordinator explorer)
    {
        this.explorer = explorer;
        tileToChar = new Dictionary<Type, char>
        {
            [typeof(Room)] = ' ',
            [typeof(Wall)] = '#',
            [typeof(Door)] = '/'
        };

        prevX = explorer.Crawler.X;
        prevY = explorer.Crawler.Y;

        explorer.DirectionChanged += DrawExplorer;
        explorer.PositionChanged += OnPositionChanged;
    }

    public void DrawLabyrinth(Labyrinth labyrinth)
    {
        SysConsole.SetCursorPosition(0, OffsetY);
        SysConsole.WriteLine(labyrinth);
    }

    private void OnPositionChanged(object? sender, CrawlingEventArgs e)
    {
        SysConsole.SetCursorPosition(prevX, prevY);
        SysConsole.Write(' ');
        DrawExplorer(sender, e);
        (prevX, prevY) = (e.X, e.Y + OffsetY);
    }

    private void DrawExplorer(object? sender, CrawlingEventArgs e)
    {
        var crawler = ((ExplorerCoordinator)sender!).Crawler;
        var facingTileType = crawler.FacingTileType.Result;

        if (facingTileType != typeof(Outside))
        {
            SysConsole.SetCursorPosition(
                e.X + e.Direction.DeltaX,
                e.Y + e.Direction.DeltaY + OffsetY
            );
            SysConsole.Write(tileToChar[facingTileType]);
        }

        SysConsole.SetCursorPosition(e.X, e.Y + OffsetY);
        SysConsole.Write(DirToChar(e.Direction));
        SysConsole.SetCursorPosition(0, 0);
        if (crawler is ClientCrawler cc)
        {
            SysConsole.WriteLine($"Bag : {cc.Bag.ItemTypes.Count()} item(s)");
        }
        Thread.Sleep(100);
    }

    private static char DirToChar(Direction dir) =>
        "^<v>"[dir.DeltaX * dir.DeltaX + dir.DeltaX + dir.DeltaY + 1];
}
