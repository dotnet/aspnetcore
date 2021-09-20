// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.BrowserTesting
{
    internal class BrowserTestOutputLogger : ITestOutputHelper
    {
        private readonly ILogger _logger;

        public BrowserTestOutputLogger(ILogger logger)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _logger = logger;
        }

        public void WriteLine(string message)
        {
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            _logger.LogInformation(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            _logger.LogInformation(string.Format(CultureInfo.InvariantCulture, format, args));
        }
    }
}
