// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenQA.Selenium;

public static class WebDriverExtensions
{
    public static IReadOnlyList<LogEntry> GetBrowserLogs(this IWebDriver driver, LogLevel level)
    {
        ArgumentNullException.ThrowIfNull(driver);

        // Fail-fast if any errors were logged to the console.
        var log = driver.Manage().Logs.GetLog(LogType.Browser);
        if (log == null)
        {
            return Array.Empty<LogEntry>();
        }

        var logs = log.Where(entry => entry.Level >= level && !ShouldIgnore(entry)).ToList();
        if (logs.Count > 0)
        {
            return logs;
        }

        return Array.Empty<LogEntry>();
    }

    // Be careful adding anything new to this list. We only want to put things here that are ignorable
    // in all cases.
    private static bool ShouldIgnore(LogEntry entry)
    {
        // Don't fail if we're missing the favicon, that's not super important.
        if (entry.Message.Contains("favicon.ico"))
        {
            return true;
        }

        // These two messages appear sometimes, but it doesn't actually block the tests.
        if (entry.Message.Contains("WASM: wasm streaming compile failed: TypeError: Could not download wasm module"))
        {
            return true;
        }
        if (entry.Message.Contains("WASM: falling back to ArrayBuffer instantiation"))
        {
            return true;
        }

        return false;
    }
}
