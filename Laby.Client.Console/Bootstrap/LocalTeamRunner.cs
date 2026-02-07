using Laby.Algorithms;
using Laby.Core;
using Laby.Core.Build;
using Laby.Core.Items;
using Laby.Client.Console.Rendering;
using Laby.Mapping;
using System.Diagnostics;
using SysConsole = System.Console;

namespace Laby.Client.Console.Bootstrap;

internal static class LocalTeamRunner
{
    private const int MaxMoves = 3000;

    public static async Task<int> RunAsync()
    {
        var labyrinth = new Labyrinth(new AsciiParser("""
                                                      +---------------+
                                                      |               |
                                                      | +----/------+ |
                                                      | |        k  |k|
                                                      | | +--/----+ | |
                                                      | / |    k  | | |
                                                      | | | +---+ | | |
                                                      | | /   |x  | | |
                                                      | | | +-+-+ | | |
                                                      | | |  k|   | | |
                                                      | | +--+--+ | | |
                                                      | |        k| | |
                                                      | +---------+-+-|
                                                      |               /
                                                      +---------------+
                                                      """));

        var sharedMap = new SharedLabyrinthMap();
        var strategies = ExplorerTeamStrategyFactory.CreateDefault();
        var explorers = strategies
            .Select(strategy => new Explorer(labyrinth.NewCrawler(), strategy, sharedMap))
            .ToArray();
        var bags = explorers.Select(_ => (Inventory)new MyInventory()).ToArray();
        var renderer = new TeamExplorerRenderer(explorers, strategies, sharedMap, labyrinth, MaxMoves);
        renderer.DrawLabyrinth();

        var durations = new TimeSpan[explorers.Length];
        var remainingMoves = await Task.WhenAll(
            explorers.Select((explorer, i) => Task.Run(async () =>
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    return await explorer.GetOut(MaxMoves, bags[i]);
                }
                finally
                {
                    stopwatch.Stop();
                    durations[i] = stopwatch.Elapsed;
                }
            }))
        );

        renderer.MoveCursorBelowViewport();
        SysConsole.WriteLine();
        SysConsole.WriteLine("Run summary:");
        for (var i = 0; i < remainingMoves.Length; i++)
        {
            var adaptive = strategies[i] as AdaptiveExplorerStrategy;
            var mode = adaptive?.CurrentStrategyName ?? strategies[i].GetType().Name;
            var status = remainingMoves[i] > 0 ? "exit reached" : "max moves";
            var elapsed = durations[i];
            var elapsedText = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}:{elapsed.Milliseconds:000}";
            SysConsole.WriteLine(
                $"Explorer #{i + 1}: remaining={remainingMoves[i]}, elapsed={elapsedText}, status={status}, mode={mode}"
            );
        }

        return remainingMoves.Any(moves => moves > 0) ? 0 : 1;
    }
}
