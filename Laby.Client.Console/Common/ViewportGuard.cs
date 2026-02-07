using Laby.Client.Console.Common;

namespace Laby.Client.Console.Common;

public static class ViewportGuard
{
    public static bool CanRender(int width, int height, int startX = 0, int startY = 0)
    {
        var bw = SafeConsole.BufferWidthSafely;
        var bh = SafeConsole.BufferHeightSafely;
        if (bw <= 0 || bh <= 0)
        {
            return false;
        }
        return startX >= 0 && startY >= 0 &&
               startX + width <= bw &&
               startY + height <= bh;
    }

    public static void ShowTooSmallMessage()
    {
        SafeConsole.TrySetCursorPosition(0, 0);
        SafeConsole.TryWriteLine("Console trop petite – affichage réduit");
    }
}
