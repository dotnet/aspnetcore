// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit.Sdk;

namespace OpenQA.Selenium;

// Used to report errors when we find errors in the browser. This is useful
// because the underlying assert probably doesn't provide good information in that
// case.
public class BrowserAssertFailedException : XunitException
{
    public BrowserAssertFailedException(IReadOnlyCollection<string> logs, Exception innerException, string screenShotPath, string innerHTML, string url = null, IReadOnlyCollection<string> networkDetails = null)
        : base(BuildMessage(innerException, logs, screenShotPath, innerHTML, url, networkDetails), innerException)
    {
    }

    private static string BuildMessage(Exception exception, IReadOnlyCollection<string> logs, string screenShotPath, string innerHTML, string url, IReadOnlyCollection<string> networkDetails)
    {
        var builder = new StringBuilder();
        builder.AppendLine(exception.ToString());

        if (!string.IsNullOrEmpty(url))
        {
            builder.AppendLine(FormattableString.Invariant($"Browser URL: {url}"));
        }

        if (File.Exists(screenShotPath))
        {
            builder.AppendLine(FormattableString.Invariant($"Screen shot captured at '{screenShotPath}'"));
        }

        if (logs.Count > 0)
        {
            builder.AppendLine("Encountered browser errors")
                .AppendJoin(Environment.NewLine, logs);
        }

        if (networkDetails is not null && networkDetails.Count > 0)
        {
            builder.AppendLine()
                .AppendLine("Network responses (_framework, _blazor, and errors):")
                .AppendJoin(Environment.NewLine, networkDetails);
        }

        builder.AppendLine("Page content:")
           .AppendLine(innerHTML);

        return builder.ToString();
    }
}
