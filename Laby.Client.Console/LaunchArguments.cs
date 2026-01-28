using System.Diagnostics.CodeAnalysis;

namespace Laby.Client.Console;

internal sealed record LaunchArguments(
    LaunchOption Option,
    Uri? ServerUri,
    string? AppKeyText,
    string? SettingsPath)
{
    public static bool IsHelp(string[] args) =>
        args.Length > 0 && IsHelpArg(args[0]);

    public static bool TryParse(
        string[] args,
        [NotNullWhen(true)] out LaunchArguments? launchArgs,
        out string? error)
    {
        launchArgs = null;
        error = null;

        if (args.Length == 0)
        {
            return false;
        }

        if (Enum.TryParse<LaunchOption>(args[0], true, out var parsedOption))
        {
            if (parsedOption == LaunchOption.Local)
            {
                launchArgs = new LaunchArguments(LaunchOption.Local, null, null, null);
                return true;
            }

            if (args.Length < 2 || !TryGetServerUri(args[1], out var serverUri))
            {
                error = "Remote mode requires a valid serverUrl.";
                return false;
            }

            var appKeyText = args.Length > 2 ? args[2] : null;
            var settingsPath = args.Length > 3 ? args[3] : null;
            launchArgs = new LaunchArguments(LaunchOption.Remote, serverUri, appKeyText, settingsPath);
            return true;
        }

        if (TryGetServerUri(args[0], out var legacyUri))
        {
            var appKeyText = args.Length > 1 ? args[1] : null;
            var settingsPath = args.Length > 2 ? args[2] : null;
            launchArgs = new LaunchArguments(LaunchOption.Remote, legacyUri, appKeyText, settingsPath);
            return true;
        }

        error = "Unknown launch option.";
        return false;
    }

    private static bool TryGetServerUri(string value, out Uri? uri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
        {
            uri = parsed;
            return true;
        }

        uri = null;
        return false;
    }

    private static bool IsHelpArg(string arg) => arg is "-h" or "--help" or "/?" or "help";
}
