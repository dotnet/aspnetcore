// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests;

public class RedisServerFixture<TStartup> : IAsyncLifetime
    where TStartup : class
{
    public InProcessTestServer<TStartup> FirstServer { get; private set; }
    public InProcessTestServer<TStartup> SecondServer { get; private set; }

    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IDisposable _logToken;

    public RedisServerFixture()
    {
        // Docker is not available on the machine, tests using this fixture
        // should be using SkipIfDockerNotPresentAttribute and will be skipped.
        if (Docker.Default == null)
        {
            return;
        }

        var testLog = AssemblyTestLog.ForAssembly(typeof(RedisServerFixture<TStartup>).Assembly);
        _logToken = testLog.StartTestLog(null, $"{nameof(RedisServerFixture<TStartup>)}_{typeof(TStartup).Name}", out _loggerFactory, LogLevel.Trace, "RedisServerFixture");
        _logger = _loggerFactory.CreateLogger<RedisServerFixture<TStartup>>();

        Docker.Default.Start(_logger);
    }

    public async Task DisposeAsync()
    {
        if (Docker.Default != null)
        {
            await FirstServer.DisposeAsync();
            await SecondServer.DisposeAsync();
            Docker.Default.Stop(_logger);
            _logToken.Dispose();
        }
    }

    public async Task InitializeAsync()
    {
        if (Docker.Default == null)
        {
            return;
        }

        FirstServer = await StartServer();
        SecondServer = await StartServer();
    }

    private async Task<InProcessTestServer<TStartup>> StartServer()
    {
        try
        {
            return await InProcessTestServer<TStartup>.StartServer(_loggerFactory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Server failed to start.");
            throw;
        }
    }
}
