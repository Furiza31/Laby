using Laby.Algorithms;
using Laby.Core;
using Laby.Core.Crawl;
using Laby.Core.Mapping;
using Laby.Core.Tiles;
using SysConsole = System.Console;

namespace Laby.Client.Console.Rendering;

internal sealed class TeamExplorerRenderer
{
    private const int StatusLineCount = 3;
    private const int HeaderY = StatusLineCount;
    private const int PanelTop = HeaderY + 1;
    private const int PanelGap = 4;
    private const int DefaultFrameDelayMs = 55;

    private readonly object _consoleLock = new();
    private readonly Explorer[] _explorers;
    private readonly IExplorerStrategy[] _strategies;
    private readonly Dictionary<Explorer, int> _explorerIndex;
    private readonly ExplorerState[] _states;
    private readonly ILabyrinthMapReader _sharedMap;
    private readonly int _maxMoves;
    private readonly char[,] _background;
    private Type[,] _observedTiles;
    private int _width;
    private int _height;
    private readonly int _observedLeft;
    private readonly bool _showLabyrinth;
    private readonly int _frameDelayMs;

    private static readonly ConsoleColor[] ExplorerColors =
    [
        ConsoleColor.Cyan,
        ConsoleColor.Yellow,
        ConsoleColor.Green
    ];

    public TeamExplorerRenderer(
        IReadOnlyList<Explorer> explorers,
        IReadOnlyList<IExplorerStrategy> strategies,
        ILabyrinthMapReader sharedMap,
        Labyrinth labyrinth,
        int maxMoves,
        bool showLabyrinth = true,
        int frameDelayMs = DefaultFrameDelayMs)
    {
        _explorers = explorers.ToArray();
        _strategies = strategies.ToArray();
        _sharedMap = sharedMap;
        _maxMoves = maxMoves;
        _showLabyrinth = showLabyrinth;
        _frameDelayMs = Math.Max(0, frameDelayMs);

        if (_explorers.Length != _strategies.Length)
        {
            throw new ArgumentException("Explorers and strategies counts must match.");
        }

        _explorerIndex = _explorers
            .Select((explorer, index) => (explorer, index))
            .ToDictionary(entry => entry.explorer, entry => entry.index);
        _states = _explorers
            .Select(explorer => new ExplorerState(
                explorer.Crawler.X,
                explorer.Crawler.Y,
                (Direction)explorer.Crawler.Direction.Clone(),
                StepsTaken: 0))
            .ToArray();

        (_background, _width, _height) = BuildBackground(labyrinth);
        _observedTiles = new Type[_width, _height];
        _observedLeft = _showLabyrinth ? _width + PanelGap : 0;

        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                _observedTiles[x, y] = typeof(Unknown);
            }
        }

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
            DrawAllStatusLines();
            DrawHeaders();

            if (_showLabyrinth)
            {
                for (var y = 0; y < _height; y++)
                {
                    SysConsole.SetCursorPosition(0, y + PanelTop);
                    for (var x = 0; x < _width; x++)
                    {
                        SysConsole.Write(_background[x, y]);
                    }
                }
            }

            DrawUnknownSharedMap();
            RefreshObservedMap();

            for (var i = 0; i < _states.Length; i++)
            {
                if (_showLabyrinth)
                {
                    DrawCell(_states[i].X, _states[i].Y);
                }
                DrawSharedCell(_states[i].X, _states[i].Y);
            }

            MoveCursorToTop();
        }
    }

    public void MoveCursorBelowViewport()
    {
        lock (_consoleLock)
        {
            var footer = Math.Min(PanelTop + _height + 1, SysConsole.BufferHeight - 1);
            SysConsole.SetCursorPosition(0, Math.Max(0, footer));
            SysConsole.ResetColor();
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
                (Direction)args.Direction.Clone(),
                previous.StepsTaken + 1
            );

            if (_showLabyrinth)
            {
                DrawCell(previous.X, previous.Y);
                DrawCell(args.X, args.Y);
            }
            RefreshObservedMap();
            DrawSharedCell(previous.X, previous.Y);
            DrawSharedCell(args.X, args.Y);
            DrawStatusLine(index);
            if (_frameDelayMs > 0)
            {
                Thread.Sleep(_frameDelayMs);
            }
            MoveCursorToTop();
        }
    }

    private void DrawCell(int x, int y)
    {
        var singleExplorer = GetSingleExplorerOnCell(x, y, out var multipleExplorers);
        SysConsole.SetCursorPosition(x, y + PanelTop);
        if (singleExplorer < 0)
        {
            SysConsole.ResetColor();
            SysConsole.Write(_background[x, y]);
            return;
        }

        if (multipleExplorers)
        {
            SysConsole.ForegroundColor = ConsoleColor.Magenta;
            SysConsole.Write('*');
            SysConsole.ResetColor();
            return;
        }

        var explorerId = singleExplorer;
        SysConsole.ForegroundColor = ExplorerColors[explorerId % ExplorerColors.Length];
        SysConsole.Write(DirectionToChar(_states[explorerId].Direction));
        SysConsole.ResetColor();
    }

    private void DrawSharedCell(int x, int y)
    {
        if (!EnsureObservedBounds(x, y))
        {
            return;
        }

        var singleExplorer = GetSingleExplorerOnCell(x, y, out var multipleExplorers);
        SysConsole.SetCursorPosition(_observedLeft + x, y + PanelTop);
        if (singleExplorer < 0)
        {
            SysConsole.ResetColor();
            SysConsole.Write(TileToChar(x, y, _observedTiles[x, y]));
            return;
        }

        if (multipleExplorers)
        {
            SysConsole.ForegroundColor = ConsoleColor.Magenta;
            SysConsole.Write('*');
            SysConsole.ResetColor();
            return;
        }

        var explorerId = singleExplorer;
        SysConsole.ForegroundColor = ExplorerColors[explorerId % ExplorerColors.Length];
        SysConsole.Write(DirectionToChar(_states[explorerId].Direction));
        SysConsole.ResetColor();
    }

    private void RefreshObservedMap()
    {
        var snapshot = _sharedMap.Snapshot();
        foreach (var entry in snapshot)
        {
            var x = entry.Key.X;
            var y = entry.Key.Y;
            if (!EnsureObservedBounds(x, y))
            {
                continue;
            }

            if (_observedTiles[x, y] == entry.Value)
            {
                continue;
            }

            _observedTiles[x, y] = entry.Value;
            DrawSharedCell(x, y);
        }
    }

    private void DrawUnknownSharedMap()
    {
        for (var y = 0; y < _height; y++)
        {
            SysConsole.SetCursorPosition(_observedLeft, y + PanelTop);
            for (var x = 0; x < _width; x++)
            {
                SysConsole.Write('?');
            }
        }
    }

    private void DrawHeaders()
    {
        if (_showLabyrinth)
        {
            WriteLine(0, HeaderY, "Labyrinth");
            WriteLine(_observedLeft, HeaderY, "Shared map (seen by explorers)");
            return;
        }

        WriteLine(0, HeaderY, "Shared map (seen by explorers)");
    }

    private void DrawAllStatusLines()
    {
        for (var i = 0; i < _states.Length; i++)
        {
            DrawStatusLine(i);
        }
    }

    private void DrawStatusLine(int index)
    {
        var remaining = Math.Max(0, _maxMoves - _states[index].StepsTaken);
        var mode = ResolveMode(index);
        var status = $"Explorer #{index + 1}: remaining={remaining}, mode={mode}";

        SysConsole.ForegroundColor = ExplorerColors[index % ExplorerColors.Length];
        WriteLine(0, index, status, clearLine: true);
        SysConsole.ResetColor();
    }

    private string ResolveMode(int index) =>
        _strategies[index] is AdaptiveExplorerStrategy adaptive
            ? adaptive.CurrentStrategyName
            : _strategies[index].GetType().Name;

    private static void WriteLine(int x, int y, string text, bool clearLine = false)
    {
        SysConsole.SetCursorPosition(x, y);
        var remainingWidth = Math.Max(0, SysConsole.BufferWidth - x - 1);
        if (remainingWidth == 0)
        {
            return;
        }

        if (text.Length > remainingWidth)
        {
            SysConsole.Write(text[..remainingWidth]);
            return;
        }

        if (clearLine)
        {
            SysConsole.Write(text.PadRight(remainingWidth));
            return;
        }

        SysConsole.Write(text);
    }

    private int GetSingleExplorerOnCell(int x, int y, out bool multipleExplorers)
    {
        var single = -1;
        multipleExplorers = false;
        for (var i = 0; i < _states.Length; i++)
        {
            if (_states[i].X != x || _states[i].Y != y)
            {
                continue;
            }

            if (single >= 0)
            {
                multipleExplorers = true;
                return single;
            }

            single = i;
        }

        return single;
    }

    private bool IsInsideBounds(int x, int y) =>
        x >= 0 && x < _width && y >= 0 && y < _height;

    private bool EnsureObservedBounds(int x, int y)
    {
        if (x < 0 || y < 0)
        {
            return false;
        }

        if (IsInsideBounds(x, y))
        {
            return true;
        }

        if (_showLabyrinth)
        {
            return false;
        }

        var oldWidth = _width;
        var oldHeight = _height;
        var newWidth = Math.Max(_width, x + 1);
        var newHeight = Math.Max(_height, y + 1);
        var resized = new Type[newWidth, newHeight];

        for (var yy = 0; yy < newHeight; yy++)
        {
            for (var xx = 0; xx < newWidth; xx++)
            {
                resized[xx, yy] = typeof(Unknown);
            }
        }

        for (var yy = 0; yy < oldHeight; yy++)
        {
            for (var xx = 0; xx < oldWidth; xx++)
            {
                resized[xx, yy] = _observedTiles[xx, yy];
            }
        }

        _observedTiles = resized;
        _width = newWidth;
        _height = newHeight;
        DrawUnknownSharedMapDelta(oldWidth, oldHeight);

        return true;
    }

    private void DrawUnknownSharedMapDelta(int oldWidth, int oldHeight)
    {
        for (var y = 0; y < _height; y++)
        {
            for (var x = 0; x < _width; x++)
            {
                if (x < oldWidth && y < oldHeight)
                {
                    continue;
                }

                SysConsole.SetCursorPosition(_observedLeft + x, y + PanelTop);
                SysConsole.ResetColor();
                SysConsole.Write('?');
            }
        }
    }

    private char TileToChar(int x, int y, Type tileType)
    {
        if (tileType == typeof(Room))
        {
            return ' ';
        }

        if (tileType == typeof(Wall))
        {
            return '#';
        }

        if (tileType == typeof(Door))
        {
            return _sharedMap.IsDoorKnownOpen(x, y) ? '=' : '/';
        }

        if (tileType == typeof(Outside))
        {
            return 'O';
        }

        return '?';
    }

    private static void MoveCursorToTop() =>
        SysConsole.SetCursorPosition(0, 0);

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

    private readonly record struct ExplorerState(int X, int Y, Direction Direction, int StepsTaken);
}
