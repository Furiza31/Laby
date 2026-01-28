using Laby.Core;
using Laby.Core.Build;
using Laby.Infrastructure.ApiClient;
using Laby.Client.Console.Arguments;
using Microsoft.Extensions.Configuration;
using Dto = Laby.Contracts;
using System.Text.Json;

namespace Laby.Client.Console.Sessions;

internal static class SessionFactory
{
    public static async Task<(SessionContext? Session, string? Error)> TryCreateAsync(LaunchArguments args)
    {
        if (args.Option == LaunchOption.Local)
        {
            var localLabyrinth = new Labyrinth(new AsciiParser("""
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
            var localCrawler = localLabyrinth.NewCrawler();
            return (new SessionContext(localLabyrinth, localCrawler, null, null), null);
        }

        var appKeyValue = args.AppKeyText;
        if (string.IsNullOrWhiteSpace(appKeyValue))
        {
            appKeyValue = UserSecretsReader.GetValue("Laby:AppKey");
        }

        if (!Guid.TryParse(appKeyValue, out var appKey))
        {
            return (null, "Missing or invalid appKey. Provide it or set user secret \"Laby:AppKey\".");
        }

        Dto.Settings? settings = null;
        if (!string.IsNullOrWhiteSpace(args.SettingsPath))
        {
            settings = JsonSerializer.Deserialize<Dto.Settings>(File.ReadAllText(args.SettingsPath));
        }

        var contest = await ContestSession.Open(args.ServerUri!, appKey, settings);
        var remoteLabyrinth = new Labyrinth(contest.Builder);
        var remoteCrawler = await contest.NewCrawler();
        var bag = contest.Bags.First();

        return (new SessionContext(remoteLabyrinth, remoteCrawler, bag, contest), null);
    }

    private static class UserSecretsReader
    {
        public static string? GetValue(string key)
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets<Program>()
                .Build();
            return config[key];
        }
    }
}
