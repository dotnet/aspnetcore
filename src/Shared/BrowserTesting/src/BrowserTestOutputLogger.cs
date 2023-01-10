// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.BrowserTesting;

internal sealed class BrowserTestOutputLogger : ITestOutputHelper
{
    private readonly ILogger _logger;

    public BrowserTestOutputLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public void WriteLine(string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        _logger.LogInformation(message);
    }

    public void WriteLine(string format, params object[] args)
    {
        _logger.LogInformation(string.Format(CultureInfo.InvariantCulture, format, args));
    }
}
