using Laby.Algorithms;
using Laby.Client.Console.Arguments;
using Laby.Client.Console.Rendering;
using Laby.Client.Console.Sessions;
using System.Diagnostics;
using SysConsole = System.Console;

namespace Laby.Client.Console.Bootstrap;

internal static class RemoteTeamRunner
{
    private const int MaxMoves = 3000;

    public static async Task<int> RunAsync(LaunchArguments args)
    {
        var (session, sessionError) = await SessionFactory.TryCreateRemoteTeamAsync(args);
        if (session is null)
        {
            UsagePrinter.Print(sessionError);
            return 1;
        }

        var renderer = new TeamExplorerRenderer(
            session.Explorers,
            session.Strategies,
            session.SharedMap,
            session.Labyrinth,
            MaxMoves,
            showLabyrinth: false,
            frameDelayMs: 0
        );

        SysConsole.Clear();
        renderer.DrawLabyrinth();

        var durations = new TimeSpan[session.Explorers.Count];
        var remainingMoves = await Task.WhenAll(
            session.Explorers.Select(
                (explorer, i) => Task.Run(async () =>
                {
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        return await explorer.GetOut(MaxMoves, session.Bags[i]);
                    }
                    finally
                    {
                        stopwatch.Stop();
                        durations[i] = stopwatch.Elapsed;
                    }
                })
            )
        );

        renderer.MoveCursorBelowViewport();
        SysConsole.WriteLine();
        SysConsole.WriteLine("Run summary:");
        for (var i = 0; i < remainingMoves.Length; i++)
        {
            var adaptive = session.Strategies[i] as AdaptiveExplorerStrategy;
            var mode = adaptive?.CurrentStrategyName ?? session.Strategies[i].GetType().Name;
            var status = remainingMoves[i] > 0 ? "exit reached" : "max moves";
            var elapsed = durations[i];
            var elapsedText = $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}:{elapsed.Milliseconds:000}";
            SysConsole.WriteLine(
                $"Explorer #{i + 1}: remaining={remainingMoves[i]}, elapsed={elapsedText}, status={status}, mode={mode}"
            );
        }

        await session.Contest.Close();
        return remainingMoves.Any(moves => moves > 0) ? 0 : 1;
    }
}
