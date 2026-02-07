using Laby.Application;
using Laby.Core;
using Laby.Core.Crawl;
using Laby.Core.Tiles;
using Laby.Infrastructure.ApiClient;
using Laby.Client.Console.Common;
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
        if (!ViewportGuard.CanRender(width: labyrinth.ToString().Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)[0].Length, height: OffsetY + 1, startX: 0, startY: OffsetY))
        {
            ViewportGuard.ShowTooSmallMessage();
            return;
        }
        SafeConsole.TrySetCursorPosition(0, OffsetY);
        SafeConsole.TryWriteLine(labyrinth.ToString());
    }

    private void OnPositionChanged(object? sender, CrawlingEventArgs e)
    {
        SafeConsole.TrySetCursorPosition(_prevX, _prevY);
        SafeConsole.TryWrite(" ");
        DrawExplorer(sender, e);
        (_prevX, _prevY) = (e.X, e.Y + OffsetY);
    }

    private void DrawExplorer(object? sender, CrawlingEventArgs e)
    {
        var crawler = ((ExplorerCoordinator)sender!).Crawler;
        var facingTileType = crawler.FacingTileType.Result;

        if (facingTileType != typeof(Outside))
        {
            SafeConsole.TrySetCursorPosition(
                e.X + e.Direction.DeltaX,
                e.Y + e.Direction.DeltaY + OffsetY
            );
            SafeConsole.TryWrite(_tileToChar[facingTileType].ToString());
        }

        SafeConsole.TrySetCursorPosition(e.X, e.Y + OffsetY);
        SafeConsole.TryWrite(DirToChar(e.Direction).ToString());
        SafeConsole.TrySetCursorPosition(0, 0);
        if (crawler is ClientCrawler cc)
        {
            SafeConsole.TryWriteLine($"Bag : {cc.Bag.ItemTypes.Count()} item(s)");
        }
        Thread.Sleep(100);
    }

    private static char DirToChar(Direction dir) =>
        "^<v>"[dir.DeltaX * dir.DeltaX + dir.DeltaX + dir.DeltaY + 1];
}
