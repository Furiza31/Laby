using SysConsole = System.Console;

namespace Laby.Client.Console;

internal static class ConsoleApp
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (LaunchArguments.IsHelp(args))
        {
            UsagePrinter.Print();
            return 0;
        }

        if (!LaunchArguments.TryParse(args, out var launchArgs, out var parseError))
        {
            UsagePrinter.Print(parseError);
            return 1;
        }

        var (session, sessionError) = await SessionFactory.TryCreateAsync(launchArgs);
        if (session is null)
        {
            UsagePrinter.Print(sessionError);
            return 1;
        }

        var renderer = new ExplorerRenderer(session.Explorer);

        SysConsole.Clear();
        renderer.DrawLabyrinth(session.Labyrinth);
        await session.Explorer.GetOut(3000, session.Bag);

        if (session.Contest is not null)
        {
            await session.Contest.Close();
        }

        return 0;
    }
}
