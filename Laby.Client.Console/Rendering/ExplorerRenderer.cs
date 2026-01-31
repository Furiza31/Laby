using Laby.Application;
using Laby.Core;
using Laby.Core.Crawl;
using Laby.Core.Tiles;
using Laby.Infrastructure.ApiClient;
using SysConsole = System.Console;

namespace Laby.Client.Console.Rendering;

internal sealed class ExplorerRenderer
{
    private const int OffsetY = 2;

    private readonly ExplorerCoordinator _explorer;
    private readonly Dictionary<Type, char> _tileToChar;
    private int _prevX;
    private int _prevY;

    public ExplorerRenderer(ExplorerCoordinator explorer)
    {
        this._explorer = explorer;
        _tileToChar = new Dictionary<Type, char>
        {
            [typeof(Room)] = ' ',
            [typeof(Wall)] = '#',
            [typeof(Door)] = '/'
        };

        _prevX = explorer.Crawler.X;
        _prevY = explorer.Crawler.Y;

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
        SysConsole.SetCursorPosition(_prevX, _prevY);
        SysConsole.Write(' ');
        DrawExplorer(sender, e);
        (_prevX, _prevY) = (e.X, e.Y + OffsetY);
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
            SysConsole.Write(_tileToChar[facingTileType]);
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
