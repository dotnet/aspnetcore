// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Tests
{
    public class WebApplicationFunctionalTests : LoggedTest
    {
        [Fact]
        public async Task LoggingConfigurationSectionPassedToLoggerByDefault()
        {
            try
            {
                await File.WriteAllTextAsync("appsettings.json", @"
{
    ""Logging"": {
        ""LogLevel"": {
            ""Default"": ""Warning""
        }
    }
}");

                await using var app = WebApplication.Create();

                var factory = (ILoggerFactory)app.Services.GetService(typeof(ILoggerFactory));
                var logger = factory.CreateLogger("Test");

                logger.Log(LogLevel.Information, 0, "Message", null, (s, e) =>
                {
                    Assert.True(false);
                    return string.Empty;
                });

                var logWritten = false;
                logger.Log(LogLevel.Warning, 0, "Message", null, (s, e) =>
                {
                    logWritten = true;
                    return string.Empty;
                });

                Assert.True(logWritten);
            }
            finally
            {
                File.Delete("appsettings.json");
            }
        }
    }
}
