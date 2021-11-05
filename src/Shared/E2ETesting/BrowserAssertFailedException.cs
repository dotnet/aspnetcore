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
    public BrowserAssertFailedException(IReadOnlyCollection<string> logs, Exception innerException, string screenShotPath, string innerHTML)
        : base(BuildMessage(innerException, logs, screenShotPath, innerHTML), innerException)
    {
    }

    private static string BuildMessage(Exception exception, IReadOnlyCollection<string> logs, string screenShotPath, string innerHTML)
    {
        var builder = new StringBuilder();
        builder.AppendLine(exception.ToString());

        if (File.Exists(screenShotPath))
        {
            builder.AppendLine(FormattableString.Invariant($"Screen shot captured at '{screenShotPath}'"));
        }

        if (logs.Count > 0)
        {
            builder.AppendLine("Encountered browser errors")
                .AppendJoin(Environment.NewLine, logs);
        }

        builder.AppendLine("Page content:")
           .AppendLine(innerHTML);

        return builder.ToString();
    }
}
