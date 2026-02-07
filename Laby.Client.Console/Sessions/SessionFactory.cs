using Laby.Algorithms;
using Laby.Core;
using Laby.Core.Build;
using Laby.Infrastructure.ApiClient;
using Laby.Client.Console.Arguments;
using Laby.Mapping;
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

        if (!TryResolveRemoteArguments(args, out var appKey, out var settings, out var error))
        {
            return (null, error);
        }

        var contest = await ContestSession.Open(args.ServerUri!, appKey, settings);
        var remoteLabyrinth = new Labyrinth(contest.Builder);
        var remoteCrawler = await contest.NewCrawler();
        var bag = contest.Bags.First();

        return (new SessionContext(remoteLabyrinth, remoteCrawler, bag, contest), null);
    }

    public static async Task<(RemoteTeamSessionContext? Session, string? Error)> TryCreateRemoteTeamAsync(LaunchArguments args)
    {
        if (!TryResolveRemoteArguments(args, out var appKey, out var settings, out var error))
        {
            return (null, error);
        }

        var contest = await ContestSession.Open(args.ServerUri!, appKey, settings);
        var remoteLabyrinth = new Labyrinth(contest.Builder);

        var strategies = ExplorerTeamStrategyFactory.CreateDefault().ToArray();
        var sharedMap = new SharedLabyrinthMap();
        var crawlers = new List<Laby.Core.Crawl.ICrawler>(strategies.Length);

        for (var i = 0; i < strategies.Length; i++)
        {
            crawlers.Add(await contest.NewCrawler());
        }

        var bags = contest.Bags.Take(strategies.Length).ToArray();
        var explorers = strategies
            .Select((strategy, index) => new Explorer(crawlers[index], strategy, sharedMap))
            .ToArray();

        return (
            new RemoteTeamSessionContext(
                remoteLabyrinth,
                contest,
                explorers,
                strategies,
                bags,
                sharedMap
            ),
            null
        );
    }

    private static bool TryResolveRemoteArguments(
        LaunchArguments args,
        out Guid appKey,
        out Dto.Settings? settings,
        out string? error)
    {
        var appKeyValue = args.AppKeyText;
        if (string.IsNullOrWhiteSpace(appKeyValue))
        {
            appKeyValue = UserSecretsReader.GetValue("Laby:AppKey");
        }

        if (!Guid.TryParse(appKeyValue, out appKey))
        {
            settings = null;
            error = "Missing or invalid appKey. Provide it or set user secret \"Laby:AppKey\".";
            return false;
        }

        settings = null;
        if (!string.IsNullOrWhiteSpace(args.SettingsPath))
        {
            settings = JsonSerializer.Deserialize<Dto.Settings>(File.ReadAllText(args.SettingsPath));
        }

        error = null;
        return true;
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
