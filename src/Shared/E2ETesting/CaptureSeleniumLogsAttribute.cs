// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using OpenQA.Selenium;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.E2ETesting;

// This has to use BeforeAfterTestAttribute because running the log capture
// in the BrowserFixture.Dispose method is too late, and we can't add logging
// to the test.
public class CaptureSeleniumLogsAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
        if (!typeof(BrowserTestBase).IsAssignableFrom(methodUnderTest.DeclaringType))
        {
            throw new InvalidOperationException("This should only be used with BrowserTestBase");
        }
    }

    public override void After(MethodInfo methodUnderTest)
    {
        var browser = BrowserTestBase.BrowserAccessor;
        var logs = BrowserTestBase.Logs;
        var output = BrowserTestBase.Output;

        if (logs != null && output != null)
        {
            // Put browser logs first, the test UI will truncate output after a certain length
            // and the browser logs will include exceptions thrown by js in the browser.
            foreach (var kind in logs.AvailableLogTypes.OrderBy(k => k == LogType.Browser ? 0 : 1))
            {
                output.WriteLine($"{kind} Logs from Selenium:");

                var entries = logs.GetLog(kind);
                foreach (LogEntry entry in entries)
                {
                    output.WriteLine($"[{entry.Timestamp}] - {entry.Level} - {entry.Message}");
                }

                output.WriteLine("");
                output.WriteLine("");
            }
        }
    }
}
