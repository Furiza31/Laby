using Laby.Client.Console.Arguments;
namespace Laby.Client.Console.Bootstrap;

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

        if (launchArgs.Option == LaunchOption.Local)
        {
            return await LocalTeamRunner.RunAsync();
        }

        return await RemoteTeamRunner.RunAsync(launchArgs);
    }
}
