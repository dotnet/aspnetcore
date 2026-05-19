// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Microsoft.AspNetCore.BrowserTesting;

public class BrowserManager
{
    private readonly BrowserManagerConfiguration _browserManagerConfiguration;
    private readonly Dictionary<string, IBrowser> _launchBrowsers = new(StringComparer.Ordinal);

    private static bool IsPlaywrightDisabled =>
#if DISABLE_PLAYWRIGHT
                                        true;
#else
                                        false;
#endif

    private object _lock = new();
    private Task _initializeTask;
    private bool _disposed;
    private readonly ILoggerFactory _loggerFactory;

    private BrowserManager(BrowserManagerConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _browserManagerConfiguration = configuration;
        _loggerFactory = loggerFactory;
    }

    public IPlaywright Playwright { get; private set; }

    public bool HasFailedTests { get; set; }

    public static async Task<BrowserManager> CreateAsync(BrowserManagerConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var manager = new BrowserManager(configuration, loggerFactory);
        await manager.InitializeAsync();

        return manager;
    }

    private async Task InitializeAsync()
    {
        await LazyInitializer.EnsureInitialized(ref _initializeTask, ref _lock, InitializeCore);

        async Task InitializeCore()
        {
            if (IsPlaywrightDisabled)
            {
                return;
            }

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            foreach (var (browserName, options) in _browserManagerConfiguration.BrowserOptions)
            {
                if (!_launchBrowsers.ContainsKey(browserName))
                {
                    var effectiveLaunchOptions = _browserManagerConfiguration.GetBrowserTypeLaunchOptions(options.BrowserLaunchOptions);

                    var browser = options.BrowserKind switch
                    {
                        BrowserKind.Chromium => await Playwright.Chromium.LaunchAsync(effectiveLaunchOptions),
                        BrowserKind.Firefox => await Playwright.Firefox.LaunchAsync(effectiveLaunchOptions),
                        BrowserKind.Webkit => await Playwright.Webkit.LaunchAsync(effectiveLaunchOptions),
                        _ => throw new InvalidOperationException("Unsupported browser type.")
                    };

                    _launchBrowsers.Add(browserName, browser);
                }
            }
        }
    }

    public IEnumerable<string> GetAvailableBrowsers() => _launchBrowsers.Keys;

    public Task<IBrowserContext> GetBrowserInstance(BrowserKind browserInstance, ContextInformation contextInfo) =>
        GetBrowserInstance(browserInstance.ToString(), contextInfo);

    public Task<IBrowserContext> GetBrowserInstance(string browserInstance, ContextInformation contextInfo)
    {
        var browser = GetBrowser(browserInstance);

        return AttachContextInfo(
            browser.NewContextAsync(contextInfo.ConfigureUniqueHarPath(_browserManagerConfiguration.GetContextOptions(browserInstance))),
            contextInfo);
    }

    public Task<IBrowserContext> GetBrowserInstance(BrowserKind browserInstance, string contextName, ContextInformation contextInfo) =>
        GetBrowserInstance(browserInstance.ToString(), contextName, contextInfo);

    public Task<IBrowserContext> GetBrowserInstance(string browserInstance, string contextName, ContextInformation contextInfo)
    {
        var browser = GetBrowser(browserInstance);

        return AttachContextInfo(
            browser.NewContextAsync(contextInfo.ConfigureUniqueHarPath(_browserManagerConfiguration.GetContextOptions(browserInstance, contextName))),
            contextInfo);
    }

    public Task<IBrowserContext> GetBrowserInstance(BrowserKind browserInstance, string contextName, BrowserNewContextOptions options, ContextInformation contextInfo) =>
        GetBrowserInstance(browserInstance.ToString(), contextName, options, contextInfo);

    public Task<IBrowserContext> GetBrowserInstance(string browserInstance, string contextName, BrowserNewContextOptions options, ContextInformation contextInfo)
    {
        var browser = GetBrowser(browserInstance);

        return AttachContextInfo(
            browser.NewContextAsync(contextInfo.ConfigureUniqueHarPath(_browserManagerConfiguration.GetContextOptions(browserInstance, contextName, options))),
            contextInfo);
    }

    private IBrowser GetBrowser(string browserInstance)
    {
        if (IsPlaywrightDisabled)
        {
            return null;
        }

        if (!_launchBrowsers.TryGetValue(browserInstance, out var browser))
        {
            throw new InvalidOperationException("Invalid browser instance.");
        }
        return browser;
    }

    private async Task<IBrowserContext> AttachContextInfo(Task<IBrowserContext> browserContextTask, ContextInformation contextInfo)
    {
        var context = await browserContextTask;
        var defaultTimeout = HasFailedTests ?
            _browserManagerConfiguration.TimeoutAfterFirstFailureInMilliseconds :
            _browserManagerConfiguration.TimeoutInMilliseconds;
        context.SetDefaultTimeout(defaultTimeout);

        contextInfo.Attach(context);
        return context;
    }

    public async Task DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var (_, browser) in _launchBrowsers)
        {
            await browser.DisposeAsync();
        }
        Playwright?.Dispose();
    }

    public bool IsAvailable(BrowserKind browserKind) =>
        _launchBrowsers.ContainsKey(browserKind.ToString());

    public bool IsExplicitlyDisabled(BrowserKind browserKind) =>
        _browserManagerConfiguration.IsDisabled ||
        _browserManagerConfiguration.DisabledBrowsers.Contains(browserKind.ToString()) ||
        IsPlaywrightDisabled;

    public static IEnumerable<object[]> WithBrowsers<T>(IEnumerable<BrowserKind> browsers, IEnumerable<T[]> data)
    {
        var result = new List<object[]>();
        foreach (var browser in browsers)
        {
            foreach (var item in data)
            {
                result.Add(item.Cast<object>().Prepend(browser).ToArray());
            }
        }

        return result;
    }

    public static IEnumerable<object[]> WithBrowsers(IEnumerable<BrowserKind> browsers, params object[] data)
    {
        var result = new List<object[]>();
        foreach (var browser in browsers)
        {
            foreach (var item in data)
            {
                result.Add(new[] { browser, item });
            }
        }

        return result;
    }
}
