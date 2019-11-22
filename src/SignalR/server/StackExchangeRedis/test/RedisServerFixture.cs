// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.SignalR.Tests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests
{
    public class RedisServerFixture<TStartup> : IDisposable
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

            FirstServer = StartServer();
            SecondServer = StartServer();
        }

        private InProcessTestServer<TStartup> StartServer()
        {
            try
            {
                return new InProcessTestServer<TStartup>(_loggerFactory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Server failed to start.");
                throw;
            }
        }

        public void Dispose()
        {
            if (Docker.Default != null)
            {
                FirstServer.Dispose();
                SecondServer.Dispose();
                Docker.Default.Stop(_logger);
                _logToken.Dispose();
            }
        }
    }
}
