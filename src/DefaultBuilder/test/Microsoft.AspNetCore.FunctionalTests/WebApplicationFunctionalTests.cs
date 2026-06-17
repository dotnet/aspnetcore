// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace Microsoft.AspNetCore.Tests;

public class WebApplicationFunctionalTests : LoggedTest
{
    [Fact]
    public async Task LoggingConfigurationSectionPassedToLoggerByDefault()
    {
        var contentRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentRootPath);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(contentRootPath, "appsettings.json"), @"
{
    ""Logging"": {
        ""LogLevel"": {
            ""Default"": ""Warning""
        }
    }
}");

            await using var app = WebApplication.Create(new[] { "--contentRoot", contentRootPath });

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
            Directory.Delete(contentRootPath, recursive: true);
        }
    }

    [Fact]
    public async Task EnvironmentSpecificLoggingConfigurationSectionPassedToLoggerByDefault()
    {
        var contentRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentRootPath);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(contentRootPath, "appsettings.Development.json"), @"
{
    ""Logging"": {
        ""LogLevel"": {
            ""Default"": ""Warning""
        }
    }
}");

            var app = WebApplication.Create(new[] { "--environment", "Development", "--contentRoot", contentRootPath });

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
            Directory.Delete(contentRootPath, recursive: true);
        }
    }

    [Fact]
    public async Task LoggingConfigurationReactsToRuntimeChanges()
    {
        var contentRootPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(contentRootPath);

        try
        {
            await File.WriteAllTextAsync(Path.Combine(contentRootPath, "appsettings.json"), @"
{
    ""Logging"": {
        ""LogLevel"": {
            ""Default"": ""Error""
        }
    }
}");

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ContentRootPath = contentRootPath,
            });

            // Disable the EventLogLoggerProvider because HostBuilder.ConfigureDefaults() configures it to log everything warning and higher which overrides non-provider-specific config.
            // https://github.com/dotnet/runtime/blob/8048fe613933a1cd91e3fad6d571c74f726143ef/src/libraries/Microsoft.Extensions.Hosting/src/HostingHostBuilderExtensions.cs#L238
            builder.Logging.AddFilter<EventLogLoggerProvider>(_ => false);

            await using var app = builder.Build();

            var factory = (ILoggerFactory)app.Services.GetService(typeof(ILoggerFactory));
            var logger = factory.CreateLogger("Test");

            Assert.False(logger.IsEnabled(LogLevel.Warning));

            logger.Log(LogLevel.Warning, 0, "Message", null, (s, e) =>
            {
                Assert.True(false);
                return string.Empty;
            });

            // Lower log level from Error to Warning and wait for logging to react to the config changes.
            var configChangedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = app.Configuration.GetReloadToken().RegisterChangeCallback(
                tcs => ((TaskCompletionSource)tcs).SetResult(), configChangedTcs);

            await File.WriteAllTextAsync(Path.Combine(contentRootPath, "appsettings.json"), @"
{
    ""Logging"": {
        ""LogLevel"": {
            ""Default"": ""Warning""
        }
    }
}");

            // Wait for a config change notification because logging will not react until this is fired. Even then, it won't react immediately
            // so we loop until success or a timeout.
            await configChangedTcs.Task.DefaultTimeout();

            var timeoutTicks = Environment.TickCount64 + InternalTesting.TaskExtensions.DefaultTimeoutDuration;
            var logWritten = false;

            while (!logWritten && Environment.TickCount < timeoutTicks)
            {
                logger.Log(LogLevel.Warning, 0, "Message", null, (s, e) =>
                {
                    logWritten = true;
                    return string.Empty;
                });
            }

            Assert.True(logWritten);
            Assert.True(logger.IsEnabled(LogLevel.Warning));
        }
        finally
        {
            Directory.Delete(contentRootPath, recursive: true);
        }
    }
}
