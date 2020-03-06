// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class HttpsTests : LoggedTest
    {
        private KestrelServerOptions CreateServerOptions()
        {
            var serverOptions = new KestrelServerOptions();
            serverOptions.ApplicationServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();
            return serverOptions;
        }

        [Fact]
        public void UseHttpsDefaultsToDefaultCert()
        {
            var serverOptions = CreateServerOptions();
            var defaultCert = TestResources.GetTestCertificate();
            serverOptions.DefaultCertificate = defaultCert;

            serverOptions.ListenLocalhost(5000, options =>
            {
                options.UseHttps();
            });

            Assert.False(serverOptions.IsDevCertLoaded);

            serverOptions.ListenLocalhost(5001, options =>
            {
                options.UseHttps(opt =>
                {
                    // The default cert is applied after UseHttps.
                    Assert.Null(opt.ServerCertificate);
                });
            });
            Assert.False(serverOptions.IsDevCertLoaded);
        }

        [Fact]
        public void ConfigureHttpsDefaultsNeverLoadsDefaultCert()
        {
            var serverOptions = CreateServerOptions();
            var testCert = TestResources.GetTestCertificate();
            serverOptions.ConfigureHttpsDefaults(options =>
            {
                Assert.Null(options.ServerCertificate);
                options.ServerCertificate = testCert;
                options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            });
            serverOptions.ListenLocalhost(5000, options =>
            {
                options.UseHttps(opt =>
                {
                    Assert.Equal(testCert, opt.ServerCertificate);
                    Assert.Equal(ClientCertificateMode.RequireCertificate, opt.ClientCertificateMode);
                });
            });
            // Never lazy loaded
            Assert.False(serverOptions.IsDevCertLoaded);
            Assert.Null(serverOptions.DefaultCertificate);
        }

        [Fact]
        public void ConfigureCertSelectorNeverLoadsDefaultCert()
        {
            var serverOptions = CreateServerOptions();
            var testCert = TestResources.GetTestCertificate();
            serverOptions.ConfigureHttpsDefaults(options =>
            {
                Assert.Null(options.ServerCertificate);
                Assert.Null(options.ServerCertificateSelector);
                options.ServerCertificateSelector = (features, name) =>
                {
                    return testCert;
                };
                options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            });
            serverOptions.ListenLocalhost(5000, options =>
            {
                options.UseHttps(opt =>
                {
                    Assert.Null(opt.ServerCertificate);
                    Assert.NotNull(opt.ServerCertificateSelector);
                    Assert.Equal(ClientCertificateMode.RequireCertificate, opt.ClientCertificateMode);
                });
            });
            // Never lazy loaded
            Assert.False(serverOptions.IsDevCertLoaded);
            Assert.Null(serverOptions.DefaultCertificate);
        }

        [Fact]
        public async Task EmptyRequestLoggedAsDebug()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            await using (var server = new TestServer(context => Task.CompletedTask,
                new TestServiceContext(LoggerFactory),
                listenOptions =>
                {
                    listenOptions.UseHttps(TestResources.GetTestCertificate());
                }))
            {
                using (var connection = server.CreateConnection())
                {
                    // Close socket immediately
                }

                await loggerProvider.FilterLogger.LogTcs.Task.DefaultTimeout();
            }

            Assert.Equal(1, loggerProvider.FilterLogger.LastEventId.Id);
            Assert.Equal(LogLevel.Debug, loggerProvider.FilterLogger.LastLogLevel);
            Assert.True(loggerProvider.ErrorLogger.TotalErrorsLogged == 0,
                userMessage: string.Join(Environment.NewLine, loggerProvider.ErrorLogger.ErrorMessages));
        }

        [Fact]
        [QuarantinedTest]
        public async Task ClientHandshakeFailureLoggedAsDebug()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            await using (var server = new TestServer(context => Task.CompletedTask,
                new TestServiceContext(LoggerFactory),
                listenOptions =>
                {
                    listenOptions.UseHttps(TestResources.GetTestCertificate());
                }))
            {
                using (var connection = server.CreateConnection())
                {
                    // Send null bytes and close socket
                    await connection.Stream.WriteAsync(new byte[10], 0, 10);
                }

                await loggerProvider.FilterLogger.LogTcs.Task.DefaultTimeout();
            }

            Assert.Equal(1, loggerProvider.FilterLogger.LastEventId.Id);
            Assert.Equal(LogLevel.Debug, loggerProvider.FilterLogger.LastLogLevel);
            Assert.True(loggerProvider.ErrorLogger.TotalErrorsLogged == 0,
                userMessage: string.Join(Environment.NewLine, loggerProvider.ErrorLogger.ErrorMessages));
        }

        // Regression test for https://github.com/aspnet/KestrelHttpServer/issues/1103#issuecomment-246971172
        [Fact]
        public async Task DoesNotThrowObjectDisposedExceptionOnConnectionAbort()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            await using (var server = new TestServer(async httpContext =>
                {
                    var ct = httpContext.RequestAborted;
                    while (!ct.IsCancellationRequested)
                    {
                        try
                        {
                            await httpContext.Response.WriteAsync($"hello, world", ct);
                            await Task.Delay(1000, ct);
                        }
                        catch (TaskCanceledException)
                        {
                            // Don't regard connection abort as an error
                        }
                    }
                },
                new TestServiceContext(LoggerFactory),
                listenOptions =>
                {
                    listenOptions.UseHttps(TestResources.GetTestCertificate());
                }))
            {
                using (var connection = server.CreateConnection())
                using (var sslStream = new SslStream(connection.Stream, true, (sender, certificate, chain, errors) => true))
                {
                    await sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                        enabledSslProtocols: SslProtocols.Tls11 | SslProtocols.Tls12,
                        checkCertificateRevocation: false);

                    var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n");
                    await sslStream.WriteAsync(request, 0, request.Length);

                    await sslStream.ReadAsync(new byte[32], 0, 32);
                }
            }

            Assert.False(loggerProvider.ErrorLogger.ObjectDisposedExceptionLogged);
        }

        [Fact]
        [Repeat(20)]
        public async Task DoesNotThrowObjectDisposedExceptionFromWriteAsyncAfterConnectionIsAborted()
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            await using (var server = new TestServer(async httpContext =>
                {
                    httpContext.Abort();
                    try
                    {
                        await httpContext.Response.WriteAsync($"hello, world");
                        tcs.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                },
                new TestServiceContext(LoggerFactory),
                listenOptions =>
                {
                    listenOptions.UseHttps(TestResources.GetTestCertificate());
                }))
            {
                using (var connection = server.CreateConnection())
                using (var sslStream = new SslStream(connection.Stream, true, (sender, certificate, chain, errors) => true))
                {
                    await sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                        enabledSslProtocols: SslProtocols.Tls11 | SslProtocols.Tls12,
                        checkCertificateRevocation: false);

                    var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n");
                    await sslStream.WriteAsync(request, 0, request.Length);

                    await sslStream.ReadAsync(new byte[32], 0, 32);
                }

                await tcs.Task.DefaultTimeout();
            }
        }

        // Regression test for https://github.com/aspnet/KestrelHttpServer/issues/1693
        [Fact]
        public async Task DoesNotThrowObjectDisposedExceptionOnEmptyConnection()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            await using (var server = new TestServer(context => Task.CompletedTask,
                new TestServiceContext(LoggerFactory),
                listenOptions =>
                {
                    listenOptions.UseHttps(TestResources.GetTestCertificate());
                }))
            {
                using (var connection = server.CreateConnection())
                using (var sslStream = new SslStream(connection.Stream, true, (sender, certificate, chain, errors) => true))
                {
                    await sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                        enabledSslProtocols: SslProtocols.Tls11 | SslProtocols.Tls12,
                        checkCertificateRevocation: false);
                }
            }

            Assert.False(loggerProvider.ErrorLogger.ObjectDisposedExceptionLogged);
        }

        // Regression test for https://github.com/aspnet/KestrelHttpServer/pull/1197
        [Fact]
        public async Task ConnectionFilterDoesNotLeakBlock()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            await using (var server = new TestServer(context => Task.CompletedTask,
                new TestServiceContext(LoggerFactory),
                listenOptions =>
                {
                    listenOptions.UseHttps(TestResources.GetTestCertificate());
                }))
            {
                using (var connection = server.CreateConnection())
                {
                    connection.Reset();
                }
            }
        }

        [Fact]
        public async Task HandshakeTimesOutAndIsLoggedAsDebug()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            var testContext = new TestServiceContext(LoggerFactory);
            var heartbeatManager = new HeartbeatManager(testContext.ConnectionManager);

            var handshakeStartedTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            TimeSpan handshakeTimeout = default;

            await using (var server = new TestServer(context => Task.CompletedTask,
                testContext,
                listenOptions =>
                {
                    listenOptions.UseHttps(o =>
                    {
                        o.ServerCertificate = new X509Certificate2(TestResources.GetTestCertificate());
                        o.OnAuthenticate = (_, __) =>
                        {
                            handshakeStartedTcs.SetResult(null);
                        };

                        handshakeTimeout = o.HandshakeTimeout;
                    });
                }))
            {
                using (var connection = server.CreateConnection())
                {
                    // HttpsConnectionAdapter dispatches via Task.Run() before starting the handshake.
                    // Wait for the handshake to start before advancing the system clock.
                    await handshakeStartedTcs.Task.DefaultTimeout();

                    // Min amount of time between requests that triggers a handshake timeout.
                    testContext.MockSystemClock.UtcNow += handshakeTimeout + Heartbeat.Interval + TimeSpan.FromTicks(1);
                    heartbeatManager.OnHeartbeat(testContext.SystemClock.UtcNow);

                    Assert.Equal(0, await connection.Stream.ReadAsync(new byte[1], 0, 1).DefaultTimeout());
                }
            }

            await loggerProvider.FilterLogger.LogTcs.Task.DefaultTimeout();
            Assert.Equal(2, loggerProvider.FilterLogger.LastEventId);
            Assert.Equal(LogLevel.Debug, loggerProvider.FilterLogger.LastLogLevel);
        }

        [Fact]
        public async Task ClientAttemptingToUseUnsupportedProtocolIsLoggedAsDebug()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            await using (var server = new TestServer(context => Task.CompletedTask,
                new TestServiceContext(LoggerFactory),
                listenOptions =>
                {
                    listenOptions.UseHttps(TestResources.GetTestCertificate("no_extensions.pfx"));
                }))
            {
                using (var connection = server.CreateConnection())
                using (var sslStream = new SslStream(connection.Stream, true, (sender, certificate, chain, errors) => true))
                {
                    // SslProtocols.Tls is TLS 1.0 which isn't supported by Kestrel by default.
                    await Assert.ThrowsAnyAsync<Exception>(() =>
                        sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                            enabledSslProtocols: SslProtocols.Tls,
                            checkCertificateRevocation: false));
                }
            }

            await loggerProvider.FilterLogger.LogTcs.Task.DefaultTimeout();
            Assert.Equal(1, loggerProvider.FilterLogger.LastEventId);
            Assert.Equal(LogLevel.Debug, loggerProvider.FilterLogger.LastLogLevel);
        }

        [Fact]
        public async Task DevCertWithInvalidPrivateKeyProducesCustomWarning()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            await using (var server = new TestServer(context => Task.CompletedTask,
                new TestServiceContext(LoggerFactory),
                listenOptions =>
                {
                    listenOptions.UseHttps(TestResources.GetTestCertificate("aspnetdevcert.pfx", "testPassword"));
                }))
            {
                using (var connection = server.CreateConnection())
                using (var sslStream = new SslStream(connection.Stream, true, (sender, certificate, chain, errors) => true))
                {
                    // SslProtocols.Tls is TLS 1.0 which isn't supported by Kestrel by default.
                    await Assert.ThrowsAnyAsync<Exception>(() =>
                        sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                            enabledSslProtocols: SslProtocols.Tls,
                            checkCertificateRevocation: false));
                }
            }

            await loggerProvider.FilterLogger.LogTcs.Task.DefaultTimeout();
            Assert.Equal(3, loggerProvider.FilterLogger.LastEventId);
            Assert.Equal(LogLevel.Error, loggerProvider.FilterLogger.LastLogLevel);
        }

        [Fact]
        public async Task OnAuthenticate_SeesOtherSettings()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            var testCert = TestResources.GetTestCertificate();
            var onAuthenticateCalled = false;

            await using (var server = new TestServer(context => Task.CompletedTask,
                new TestServiceContext(LoggerFactory),
                listenOptions =>
                {
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.ServerCertificate = testCert;
                        httpsOptions.OnAuthenticate = (connectionContext, authOptions) =>
                        {
                            Assert.Same(testCert, authOptions.ServerCertificate);
                            onAuthenticateCalled = true;
                        };
                    });
                }))
            {
                using (var connection = server.CreateConnection())
                using (var sslStream = new SslStream(connection.Stream, true, (sender, certificate, chain, errors) => true))
                {
                    await sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                            enabledSslProtocols: SslProtocols.Tls11 | SslProtocols.Tls12,
                            checkCertificateRevocation: false);
                }
            }

            Assert.True(onAuthenticateCalled, "onAuthenticateCalled");
        }

        [Fact]
        public async Task OnAuthenticate_CanSetSettings()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            var testCert = TestResources.GetTestCertificate();
            var onAuthenticateCalled = false;

            await using (var server = new TestServer(context => Task.CompletedTask,
                new TestServiceContext(LoggerFactory),
                listenOptions =>
                {
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.ServerCertificateSelector = (_, __) => throw new NotImplementedException();
                        httpsOptions.OnAuthenticate = (connectionContext, authOptions) =>
                        {
                            Assert.Null(authOptions.ServerCertificate);
                            Assert.NotNull(authOptions.ServerCertificateSelectionCallback);
                            authOptions.ServerCertificate = testCert;
                            authOptions.ServerCertificateSelectionCallback = null;
                            onAuthenticateCalled = true;
                        };
                    });
                }))
            {
                using (var connection = server.CreateConnection())
                using (var sslStream = new SslStream(connection.Stream, true, (sender, certificate, chain, errors) => true))
                {
                    await sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                            enabledSslProtocols: SslProtocols.Tls11 | SslProtocols.Tls12,
                            checkCertificateRevocation: false);
                }
            }

            Assert.True(onAuthenticateCalled, "onAuthenticateCalled");
        }

        private class HandshakeErrorLoggerProvider : ILoggerProvider
        {
            public HttpsConnectionFilterLogger FilterLogger { get; } = new HttpsConnectionFilterLogger();
            public ApplicationErrorLogger ErrorLogger { get; } = new ApplicationErrorLogger();

            public ILogger CreateLogger(string categoryName)
            {
                if (categoryName == TypeNameHelper.GetTypeDisplayName(typeof(HttpsConnectionMiddleware)))
                {
                    return FilterLogger;
                }
                else
                {
                    return ErrorLogger;
                }
            }

            public void Dispose()
            {
            }
        }

        private class HttpsConnectionFilterLogger : ILogger
        {
            public LogLevel LastLogLevel { get; set; }
            public EventId LastEventId { get; set; }
            public TaskCompletionSource<object> LogTcs { get; } = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

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
            private List<string> _errorMessages = new List<string>();

            public IEnumerable<string> ErrorMessages => _errorMessages;

            public int TotalErrorsLogged => _errorMessages.Count;

            public bool ObjectDisposedExceptionLogged { get; set; }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (logLevel == LogLevel.Error)
                {
                    var log = $"Log {logLevel}[{eventId}]: {formatter(state, exception)} {exception}";
                    _errorMessages.Add(log);
                }

                if (exception is ObjectDisposedException)
                {
                    ObjectDisposedExceptionLogged = true;
                }
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return NullScope.Instance;
            }
        }
    }
}
