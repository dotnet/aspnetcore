// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class HttpsTests
    {
        [Fact]
        public async Task EmptyRequestLoggedAsInformation()
        {
            var loggerFactory = new HandshakeErrorLoggerFactory();

            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.UseHttps(@"TestResources/testCert.pfx", "testPassword");
                })
                .UseUrls("https://127.0.0.1:0/")
                .UseLoggerFactory(loggerFactory)
                .Configure(app => { });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (await HttpClientSlim.GetSocket(new Uri($"http://127.0.0.1:{host.GetPort()}/")))
                {
                    // Close socket immediately
                }

                await loggerFactory.FilterLogger.LogTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            }

            Assert.Equal(1, loggerFactory.FilterLogger.LastEventId.Id);
            Assert.Equal(LogLevel.Information, loggerFactory.FilterLogger.LastLogLevel);
            Assert.Equal(0, loggerFactory.ErrorLogger.TotalErrorsLogged);
        }

        [Fact]
        public async Task ClientHandshakeFailureLoggedAsInformation()
        {
            var loggerFactory = new HandshakeErrorLoggerFactory();

            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.UseHttps(@"TestResources/testCert.pfx", "testPassword");
                })
                .UseUrls("https://127.0.0.1:0/")
                .UseLoggerFactory(loggerFactory)
                .Configure(app => { });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var socket = await HttpClientSlim.GetSocket(new Uri($"https://127.0.0.1:{host.GetPort()}/")))
                using (var stream = new NetworkStream(socket))
                {
                    // Send null bytes and close socket
                    await stream.WriteAsync(new byte[10], 0, 10);
                }

                await loggerFactory.FilterLogger.LogTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            }

            Assert.Equal(1, loggerFactory.FilterLogger.LastEventId.Id);
            Assert.Equal(LogLevel.Information, loggerFactory.FilterLogger.LastLogLevel);
            Assert.Equal(0, loggerFactory.ErrorLogger.TotalErrorsLogged);
        }

        private class HandshakeErrorLoggerFactory : ILoggerFactory
        {
            public HttpsConnectionFilterLogger FilterLogger { get; } = new HttpsConnectionFilterLogger();
            public ApplicationErrorLogger ErrorLogger { get; } = new ApplicationErrorLogger();

            public ILogger CreateLogger(string categoryName)
            {
                if (categoryName == nameof(HttpsConnectionFilter))
                {
                    return FilterLogger;
                }
                else
                {
                    return ErrorLogger;
                }
            }

            public void AddProvider(ILoggerProvider provider)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }
        }

        private class HttpsConnectionFilterLogger : ILogger
        {
            public LogLevel LastLogLevel { get; set; }
            public EventId LastEventId { get; set; }
            public TaskCompletionSource<object> LogTcs { get; } = new TaskCompletionSource<object>();

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                LastLogLevel = logLevel;
                LastEventId = eventId;
                LogTcs.SetResult(null);
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                throw new NotImplementedException();
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }
        }

        private class ApplicationErrorLogger : ILogger
        {
            public int TotalErrorsLogged { get; set; }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (logLevel == LogLevel.Error)
                {
                    TotalErrorsLogged++;
                }
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                throw new NotImplementedException();
            }
        }
    }
}
