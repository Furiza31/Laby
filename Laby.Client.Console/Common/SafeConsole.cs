using System;
using System.Text;

namespace Laby.Client.Console.Common;

public static class SafeConsole
{
    public static bool IsInteractive =>
        !(System.Console.IsOutputRedirected || System.Console.IsErrorRedirected || System.Console.IsInputRedirected);

    public static int BufferWidthSafely
    {
        get
        {
            try { return System.Console.BufferWidth; }
            catch { return 80; }
        }
    }

    public static int BufferHeightSafely
    {
        get
        {
            try { return System.Console.BufferHeight; }
            catch { return 25; }
        }
    }

    public static void TrySetCursorPosition(int x, int y)
    {
        if (!IsInteractive)
        {
            return;
        }

        var bw = BufferWidthSafely;
        var bh = BufferHeightSafely;
        if (bw <= 0 || bh <= 0)
        {
            return;
        }

        var cx = Math.Clamp(x, 0, bw - 1);
        var cy = Math.Clamp(y, 0, bh - 1);
        try
        {
            System.Console.SetCursorPosition(cx, cy);
        }
        catch
        {
        }
    }

    public static void TryWrite(string text)
    {
        if (text is null)
        {
            return;
        }

        if (!IsInteractive)
        {
            System.Console.Write(text);
            return;
        }

        var left = SafeGetCursorLeft();
        var bw = BufferWidthSafely;
        var remainingWidth = Math.Max(0, bw - left - 1);
        if (remainingWidth == 0)
        {
            return;
        }

        if (text.Length > remainingWidth)
        {
            System.Console.Write(text.Substring(0, remainingWidth));
            return;
        }

        System.Console.Write(text);
    }

    public static void TryWriteLine(string text)
    {
        if (text is null)
        {
            return;
        }

        if (!IsInteractive)
        {
            System.Console.WriteLine(text);
            return;
        }

        var left = SafeGetCursorLeft();
        var bw = BufferWidthSafely;
        var remainingWidth = Math.Max(0, bw - left - 1);
        if (remainingWidth == 0)
        {
            return;
        }

        if (text.Length > remainingWidth)
        {
            System.Console.WriteLine(text.Substring(0, remainingWidth));
            return;
        }

        System.Console.WriteLine(text);
    }

    public static void TryWritePadded(string text, int totalWidth)
    {
        if (text is null)
        {
            return;
        }

        if (!IsInteractive)
        {
            System.Console.Write(text);
            return;
        }

        var left = SafeGetCursorLeft();
        var bw = BufferWidthSafely;
        var remainingWidth = Math.Max(0, Math.Min(totalWidth, bw - left - 1));
        if (remainingWidth == 0)
        {
            return;
        }

        var toWrite = text.Length > remainingWidth ? text.Substring(0, remainingWidth) : text;
        System.Console.Write(toWrite.PadRight(remainingWidth));
    }

    private static int SafeGetCursorLeft()
    {
        try { return System.Console.CursorLeft; }
        catch { return 0; }
    }

    public static void ResetColorSafely()
    {
        try { System.Console.ResetColor(); }
        catch { }
    }

    public static void SetForegroundColorSafely(ConsoleColor color)
    {
        try { System.Console.ForegroundColor = color; }
        catch { }
    }
}
