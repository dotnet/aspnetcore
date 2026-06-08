// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Per-test Playwright attachment helpers that route artifacts into MSTest's
/// <see cref="TestContext.AddResultFile(string)"/> so they appear as
/// <c>&lt;ResultFile&gt;</c> entries under the matching <c>&lt;UnitTestResult&gt;</c> in the
/// generated TRX report.
/// </summary>
public static class PlaywrightAttachmentExtensions
{
    /// <summary>
    /// Takes a full-page screenshot of <paramref name="page"/>, writes it to a temp file,
    /// and attaches it to <paramref name="test"/>'s result.
    /// </summary>
    /// <param name="page">The Playwright page to screenshot.</param>
    /// <param name="test">The MSTest test context for the currently running test.</param>
    /// <param name="name">A suggested file name (a unique prefix is added).</param>
    public static async Task AttachScreenshotAsync(this IPage page, TestContext test, string name = "screenshot.png")
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(test);

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-" + name);
        await page.ScreenshotAsync(new() { Path = path, FullPage = true }).ConfigureAwait(false);
        test.AddResultFile(path);
    }

    /// <summary>
    /// Subscribes to <see cref="IPage.Console"/> events on <paramref name="page"/>,
    /// writes them line-by-line to a temp file, and on disposal attaches the file
    /// to <paramref name="test"/>'s result.
    /// </summary>
    /// <param name="page">The Playwright page to capture browser-console output from.</param>
    /// <param name="test">The MSTest test context for the currently running test.</param>
    /// <param name="logName">A suggested file name (a unique prefix is added).</param>
    /// <returns>
    /// An <see cref="IDisposable"/> that, when disposed, unsubscribes the console
    /// handler, flushes the file, and attaches it.
    /// </returns>
    public static IDisposable CaptureBrowserConsole(this IPage page, TestContext test, string logName = "console.log")
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(test);

        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + "-" + logName);
        var writer = new StreamWriter(path) { AutoFlush = true };

        void Handler(object? _, IConsoleMessage msg) => writer.WriteLine($"[{msg.Type}] {msg.Text}");
        page.Console += Handler;

        return new ConsoleCapture(page, Handler, writer, path, test);
    }

    private sealed class ConsoleCapture : IDisposable
    {
        readonly IPage _page;
        readonly EventHandler<IConsoleMessage> _handler;
        readonly StreamWriter _writer;
        readonly string _path;
        readonly TestContext _test;

        public ConsoleCapture(IPage page, EventHandler<IConsoleMessage> handler, StreamWriter writer, string path, TestContext test)
        {
            _page = page;
            _handler = handler;
            _writer = writer;
            _path = path;
            _test = test;
        }

        public void Dispose()
        {
            try
            {
                _page.Console -= _handler;
            }
            catch
            {
                // page may have been closed
            }

            _writer.Dispose();
            _test.AddResultFile(_path);
        }
    }
}
