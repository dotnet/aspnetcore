// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.E2ETesting;

[CaptureSeleniumLogs]
public class BrowserTestBase : IClassFixture<BrowserFixture>, IAsyncLifetime
{
    private static readonly AsyncLocal<IWebDriver> _asyncBrowser = new AsyncLocal<IWebDriver>();
    private static readonly AsyncLocal<ILogs> _logs = new AsyncLocal<ILogs>();
    private static readonly AsyncLocal<ITestOutputHelper> _output = new AsyncLocal<ITestOutputHelper>();

    private ExceptionDispatchInfo _exceptionDispatchInfo;
    private IWebDriver _browser;

    public BrowserTestBase(BrowserFixture browserFixture, ITestOutputHelper output)
    {
        BrowserFixture = browserFixture;
        _output.Value = output;
    }

    public IWebDriver Browser
    {
        get
        {
            if (_exceptionDispatchInfo != null)
            {
                _exceptionDispatchInfo.Throw();
                throw _exceptionDispatchInfo.SourceException;
            }

            return _browser;
        }
        set
        {
            _browser = value;
        }
    }

    public static IWebDriver BrowserAccessor => _asyncBrowser.Value;

    public static ILogs Logs => _logs.Value;

    public static ITestOutputHelper Output => _output.Value;

    public BrowserFixture BrowserFixture { get; }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public virtual Task InitializeAsync()
    {
        return InitializeAsync("");
    }

    public virtual Task InitializeAsync(string isolationContext)
    {
        InitializeBrowser(isolationContext);
        InitializeAsyncCore();
        return Task.CompletedTask;
    }

    protected virtual void InitializeAsyncCore()
    {
    }

    protected void InitializeBrowser(string isolationContext)
    {
        try
        {
            var (browser, logs) = BrowserFixture.GetOrCreateBrowser(Output, isolationContext);
            _asyncBrowser.Value = browser;
            _logs.Value = logs;

            Browser = browser;
        }
        catch (Exception ex)
        {
            _exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
            throw;
        }
    }
}
