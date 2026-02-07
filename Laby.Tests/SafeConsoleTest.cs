using System;
using System.IO;
using Laby.Client.Console.Common;
using NUnit.Framework;

namespace Laby.Tests;

public class SafeConsoleTest
{
    [Test]
    public void TrySetCursorPosition_DoesNotThrow_WhenOutputRedirected()
    {
        using var writer = new StringWriter();
        var originalOut = System.Console.Out;
        try
        {
            System.Console.SetOut(writer);
            SafeConsole.TrySetCursorPosition(1000, 1000);
            SafeConsole.TryWrite("hello");
            SafeConsole.TryWriteLine("world");
        }
        finally
        {
            System.Console.SetOut(originalOut);
        }
        Assert.Pass();
    }

    [Test]
    public void ViewportGuard_ReturnsFalse_WhenTooLarge()
    {
        var bw = SafeConsole.BufferWidthSafely;
        var bh = SafeConsole.BufferHeightSafely;
        Assert.That(ViewportGuard.CanRender(bw + 100, bh + 100), Is.False);
    }
}
