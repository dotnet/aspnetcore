// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public class HttpsTests
    {
        [Fact]
        public async Task EmptyRequestLoggedAsInformation()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();

            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
                .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
                .Configure(app => { });

            using (var host = hostBuilder.Build())
            {
                host.Start();

                using (await HttpClientSlim.GetSocket(new Uri($"http://127.0.0.1:{host.GetPort()}/")))
                {
                    // Close socket immediately
                }

                await loggerProvider.FilterLogger.LogTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            }

            Assert.Equal(1, loggerProvider.FilterLogger.LastEventId.Id);
            Assert.Equal(LogLevel.Information, loggerProvider.FilterLogger.LastLogLevel);
            Assert.True(loggerProvider.ErrorLogger.TotalErrorsLogged == 0,
                userMessage: string.Join(Environment.NewLine, loggerProvider.ErrorLogger.ErrorMessages));
        }

        [Fact]
        public async Task ClientHandshakeFailureLoggedAsInformation()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();

            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
                .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
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

                await loggerProvider.FilterLogger.LogTcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
            }

            Assert.Equal(1, loggerProvider.FilterLogger.LastEventId.Id);
            Assert.Equal(LogLevel.Information, loggerProvider.FilterLogger.LastLogLevel);
            Assert.True(loggerProvider.ErrorLogger.TotalErrorsLogged == 0,
                userMessage: string.Join(Environment.NewLine, loggerProvider.ErrorLogger.ErrorMessages));
        }

        // Regression test for https://github.com/aspnet/KestrelHttpServer/issues/1103#issuecomment-246971172
        [Fact]
        public async Task DoesNotThrowObjectDisposedExceptionOnConnectionAbort()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
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

        [Fact]
        public async Task DoesNotThrowObjectDisposedExceptionFromWriteAsyncAfterConnectionIsAborted()
        {
            var tcs = new TaskCompletionSource<object>();
            var loggerProvider = new HandshakeErrorLoggerProvider();
            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
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
                    await sslStream.ReadAsync(new byte[32], 0, 32);
                }
            }

            await tcs.Task.TimeoutAfter(TimeSpan.FromSeconds(10));
        }

        // Regression test for https://github.com/aspnet/KestrelHttpServer/issues/1693
        [Fact]
        public async Task DoesNotThrowObjectDisposedExceptionOnEmptyConnection()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();
            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
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
        [Fact]
        public void ConnectionFilterDoesNotLeakBlock()
        {
            var loggerProvider = new HandshakeErrorLoggerProvider();

            var hostBuilder = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(new IPEndPoint(IPAddress.Loopback, 0), listenOptions =>
                    {
                        listenOptions.UseHttps(TestResources.TestCertificatePath, "testPassword");
                    });
                })
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
            public TaskCompletionSource<object> LogTcs { get; } = new TaskCompletionSource<object>();

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                LastLogLevel = logLevel;
                LastEventId = eventId;
                Task.Run(() => LogTcs.SetResult(null));
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
                    _errorMessages.Add(formatter(state, exception));
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
