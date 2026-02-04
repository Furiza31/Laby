using Laby.Algorithms;
using Laby.Core;
using Laby.Core.Crawl;
using SysConsole = System.Console;

namespace Laby.Client.Console.Rendering;

internal sealed class TeamExplorerRenderer
{
    private const int OffsetY = 2;
    private readonly object _consoleLock = new();
    private readonly Explorer[] _explorers;
    private readonly Dictionary<Explorer, int> _explorerIndex;
    private readonly ExplorerState[] _states;
    private readonly char[,] _background;
    private readonly int _width;
    private readonly int _height;

    private static readonly ConsoleColor[] ExplorerColors =
    [
        ConsoleColor.Cyan,
        ConsoleColor.Yellow,
        ConsoleColor.Green
    ];

    public TeamExplorerRenderer(IReadOnlyList<Explorer> explorers, Labyrinth labyrinth)
    {
        _explorers = explorers.ToArray();
        _explorerIndex = _explorers
            .Select((explorer, index) => (explorer, index))
            .ToDictionary(entry => entry.explorer, entry => entry.index);
        _states = _explorers
            .Select(explorer => new ExplorerState(
                explorer.Crawler.X,
                explorer.Crawler.Y,
                (Direction)explorer.Crawler.Direction.Clone()))
            .ToArray();

        (_background, _width, _height) = BuildBackground(labyrinth);

        foreach (var explorer in _explorers)
        {
            explorer.PositionChanged += OnExplorerChanged;
            explorer.DirectionChanged += OnExplorerChanged;
        }
    }

    public void DrawLabyrinth()
    {
        lock (_consoleLock)
        {
            SysConsole.Clear();
            SysConsole.WriteLine("Local collaborative mode (3 explorers)");
            for (var y = 0; y < _height; y++)
            {
                SysConsole.SetCursorPosition(0, y + OffsetY);
                for (var x = 0; x < _width; x++)
                {
                    SysConsole.Write(_background[x, y]);
                }
            }

            for (var i = 0; i < _states.Length; i++)
            {
                DrawCell(_states[i].X, _states[i].Y);
            }
        }
    }

    private void OnExplorerChanged(object? sender, CrawlingEventArgs args)
    {
        if (sender is not Explorer explorer ||
            !_explorerIndex.TryGetValue(explorer, out var index))
        {
            return;
        }

        lock (_consoleLock)
        {
            var previous = _states[index];
            _states[index] = new ExplorerState(
                args.X,
                args.Y,
                (Direction)args.Direction.Clone()
            );

            DrawCell(previous.X, previous.Y);
            DrawCell(args.X, args.Y);
            SysConsole.SetCursorPosition(0, 0);
            Thread.Sleep(60);
        }
    }

    private void DrawCell(int x, int y)
    {
        var explorersOnCell = _states
            .Select((state, index) => (state, index))
            .Where(entry => entry.state.X == x && entry.state.Y == y)
            .Select(entry => entry.index)
            .ToArray();

        SysConsole.SetCursorPosition(x, y + OffsetY);
        if (explorersOnCell.Length == 0)
        {
            SysConsole.ResetColor();
            SysConsole.Write(_background[x, y]);
            return;
        }

        if (explorersOnCell.Length > 1)
        {
            SysConsole.ForegroundColor = ConsoleColor.Magenta;
            SysConsole.Write('*');
            SysConsole.ResetColor();
            return;
        }

        var explorerId = explorersOnCell[0];
        SysConsole.ForegroundColor = ExplorerColors[explorerId % ExplorerColors.Length];
        SysConsole.Write(DirectionToChar(_states[explorerId].Direction));
        SysConsole.ResetColor();
    }

    private static (char[,] Grid, int Width, int Height) BuildBackground(Labyrinth labyrinth)
    {
        var lines = labyrinth
            .ToString()
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        var height = lines.Length;
        var width = lines[0].Length;
        var grid = new char[width, height];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                grid[x, y] = lines[y][x];
            }
        }

        return (grid, width, height);
    }

    private static char DirectionToChar(Direction direction) =>
        "^<v>"[direction.DeltaX * direction.DeltaX + direction.DeltaX + direction.DeltaY + 1];

    private readonly record struct ExplorerState(int X, int Y, Direction Direction);
}
