using SysConsole = System.Console;

namespace Laby.Client.Console;

internal static class UsagePrinter
{
    public static void Print(string? error = null)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            SysConsole.WriteLine($"Error: {error}");
            SysConsole.WriteLine();
        }

        SysConsole.WriteLine("""
Usage:
  Laby.Client.Console local
  Laby.Client.Console remote <serverUrl> [appKeyGuid] [settings.json]

Notes:
  - If appKeyGuid is omitted in remote mode, it is read from user secrets key "Laby:AppKey".
  - Legacy: you can still call with "<serverUrl> <appKeyGuid> [settings.json]" (without "remote").
""");
    }
}
