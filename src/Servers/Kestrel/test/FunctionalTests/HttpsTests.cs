// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

using static Microsoft.AspNetCore.Server.Kestrel.FunctionalTests.FinOnErrorHelpers;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
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
            var defaultCert = new X509Certificate2(TestResources.TestCertificatePath, "testPassword");
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
            var testCert = new X509Certificate2(TestResources.TestCertificatePath, "testPassword");
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
            var testCert = new X509Certificate2(TestResources.TestCertificatePath, "testPassword");
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

            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
                .ConfigureServices(AddTestLogging)
                .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
                .Configure(app => { });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (await HttpClientSlim.GetSocket(new Uri($"http://127.0.0.1:{host.GetPort()}/")))
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
        public async Task ClientHandshakeFailureLoggedAsDebug()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
                .ConfigureServices(AddTestLogging)
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
            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
                .ConfigureServices(AddTestLogging)
                .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
                .Configure(app => app.Run(async httpContext =>
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
                }));

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var socket = await HttpClientSlim.GetSocket(new Uri($"https://127.0.0.1:{host.GetPort()}/")))
                using (var stream = new NetworkStream(socket, ownsSocket: false))
                using (var sslStream = new SslStream(stream, true, (sender, certificate, chain, errors) => true))
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task DoesNotThrowObjectDisposedExceptionFromWriteAsyncAfterConnectionIsAborted(bool fin)
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);
            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
                .ConfigureServices(services => SetFinOnError(services, fin))
                .ConfigureServices(AddTestLogging)
                .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
                .Configure(app => app.Run(async httpContext =>
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
                }));

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var socket = await HttpClientSlim.GetSocket(new Uri($"https://127.0.0.1:{host.GetPort()}/")))
                using (var stream = new NetworkStream(socket, ownsSocket: false))
                using (var sslStream = new SslStream(stream, true, (sender, certificate, chain, errors) => true))
                {
                    await sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                        enabledSslProtocols: SslProtocols.Tls11 | SslProtocols.Tls12,
                        checkCertificateRevocation: false);

                    var request = Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost:\r\n\r\n");
                    await sslStream.WriteAsync(request, 0, request.Length);

                    if (ExpectFinOnError(fin))
                    {
                        await sslStream.ReadAsync(new byte[32], 0, 32);
                    }
                    else
                    {
                        await Assert.ThrowsAsync<IOException>(() => sslStream.ReadAsync(new byte[32], 0, 32));
                    }
                }
            }

            await tcs.Task.DefaultTimeout();
        }

        // Regression test for https://github.com/aspnet/KestrelHttpServer/issues/1693
        [Fact]
        public async Task DoesNotThrowObjectDisposedExceptionOnEmptyConnection()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);
            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
                .ConfigureServices(AddTestLogging)
                .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
                .Configure(app => app.Run(httpContext => Task.CompletedTask));

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var socket = await HttpClientSlim.GetSocket(new Uri($"https://127.0.0.1:{host.GetPort()}/")))
                using (var stream = new NetworkStream(socket, ownsSocket: false))
                using (var sslStream = new SslStream(stream, true, (sender, certificate, chain, errors) => true))
                {
                    await sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                        enabledSslProtocols: SslProtocols.Tls11 | SslProtocols.Tls12,
                        checkCertificateRevocation: false);
                }
            }

            Assert.False(loggerProvider.ErrorLogger.ObjectDisposedExceptionLogged);
        }

        // Regression test for https://github.com/aspnet/KestrelHttpServer/pull/1197
        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "macOS EPIPE vs. EPROTOTYPE bug https://github.com/aspnet/KestrelHttpServer/issues/2885")]
        public void ConnectionFilterDoesNotLeakBlock()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);

            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
                .ConfigureServices(AddTestLogging)
                .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
                .Configure(app => { });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(new IPEndPoint(IPAddress.Loopback, host.GetPort()));

                    // Close socket immediately
                    socket.LingerState = new LingerOption(true, 0);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task HandshakeTimesOutAndIsLoggedAsDebug(bool fin)
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            LoggerFactory.AddProvider(loggerProvider);
            var hostBuilder = TransportSelector.GetWebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(o =>
                        {
                            o.ServerCertificate = new X509Certificate2(TestResources.TestCertificatePath, "testPassword");
                            o.HandshakeTimeout = TimeSpan.FromSeconds(1);
                        });
                    });
                })
                .ConfigureServices(services => SetFinOnError(services, fin))
                .ConfigureServices(AddTestLogging)
                .Configure(app => app.Run(httpContext => Task.CompletedTask));

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (var socket = await HttpClientSlim.GetSocket(new Uri($"https://127.0.0.1:{host.GetPort()}/")))
                using (var stream = new NetworkStream(socket, ownsSocket: false))
                {
                    if (ExpectFinOnError(fin))
                    {
                        // No data should be sent and the connection should be closed in well under 30 seconds.
                        Assert.Equal(0, await stream.ReadAsync(new byte[1], 0, 1).DefaultTimeout());
                    }
                    else
                    {
                        await Assert.ThrowsAsync<IOException>(() => stream.ReadAsync(new byte[1], 0, 1)).DefaultTimeout();
                    }
                }
            }

            await loggerProvider.FilterLogger.LogTcs.Task.DefaultTimeout();
            Assert.Equal(2, loggerProvider.FilterLogger.LastEventId);
            Assert.Equal(LogLevel.Debug, loggerProvider.FilterLogger.LastLogLevel);
        }

        private class HandshakeErrorLoggerProvider : ILoggerProvider
        {
            public HttpsConnectionFilterLogger FilterLogger { get; } = new HttpsConnectionFilterLogger();
            public ApplicationErrorLogger ErrorLogger { get; } = new ApplicationErrorLogger();

            public ILogger CreateLogger(string categoryName)
            {
                if (categoryName == nameof(HttpsConnectionAdapter))
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
