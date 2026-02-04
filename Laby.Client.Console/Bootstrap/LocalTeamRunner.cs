using Laby.Algorithms;
using Laby.Core;
using Laby.Core.Build;
using Laby.Core.Items;
using Laby.Client.Console.Rendering;
using Laby.Mapping;
using SysConsole = System.Console;

namespace Laby.Client.Console.Bootstrap;

internal static class LocalTeamRunner
{
    private const int MaxMoves = 3000;

    public static async Task<int> RunAsync()
    {
        var labyrinth = new Labyrinth(new AsciiParser("""
                                                      +--+--------+
                                                      |  /        |
                                                      |  +--+--+  |
                                                      |     |k    |
                                                      +--+  |  +--+
                                                         |k  x    |
                                                      +  +-------/|
                                                      |           |
                                                      +-----------+
                                                      """));

        var sharedMap = new SharedLabyrinthMap();
        var strategies = ExplorerTeamStrategyFactory.CreateDefault();
        var explorers = strategies
            .Select(strategy => new Explorer(labyrinth.NewCrawler(), strategy, sharedMap))
            .ToArray();
        var bags = explorers.Select(_ => (Inventory)new MyInventory()).ToArray();
        var renderer = new TeamExplorerRenderer(explorers, labyrinth);
        renderer.DrawLabyrinth();

        var remainingMoves = await Task.WhenAll(
            explorers.Select((explorer, i) => Task.Run(() => explorer.GetOut(MaxMoves, bags[i])))
        );

        SysConsole.WriteLine();
        SysConsole.WriteLine("Run summary:");
        for (var i = 0; i < remainingMoves.Length; i++)
        {
            var adaptive = strategies[i] as AdaptiveExplorerStrategy;
            var mode = adaptive?.CurrentStrategyName ?? strategies[i].GetType().Name;
            SysConsole.WriteLine(
                $"Explorer #{i + 1}: remaining={remainingMoves[i]}, mode={mode}"
            );
        }

        return remainingMoves.Any(moves => moves > 0) ? 0 : 1;
    }
}
