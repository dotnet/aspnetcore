// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class HttpsTests : LoggedTest
{
    private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();

    private static KestrelServerOptions CreateServerOptions()
    {
        // It's not actually going to be used - we just need to satisfy the check in ApplyDefaultCertificate
        var mockHttpsConfig = new Mock<IHttpsConfigurationService>();
        mockHttpsConfig.Setup(m => m.IsInitialized).Returns(true);

        var serverOptions = new KestrelServerOptions();
        serverOptions.ApplicationServices = new ServiceCollection()
            .AddLogging()
            .AddSingleton(mockHttpsConfig.Object)
            .AddSingleton(Mock.Of<IHostEnvironment>())
            .AddSingleton(new KestrelMetrics(new TestMeterFactory()))
            .BuildServiceProvider();
        return serverOptions;
    }

    [Fact]
    public void UseHttpsDefaultsToDefaultCert()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.TestOverrideDefaultCertificate = _x509Certificate2;

        serverOptions.ListenLocalhost(5000, options =>
        {
            options.UseHttps();
        });

        Assert.False(serverOptions.IsDevelopmentCertificateLoaded);

        var ranUseHttpsAction = false;
        serverOptions.ListenLocalhost(5001, options =>
        {
            options.UseHttps(opt =>
            {
                // The default cert is applied after UseHttps.
                Assert.Null(opt.ServerCertificate);
                ranUseHttpsAction = true;
            });
        });

        Assert.True(ranUseHttpsAction);
        Assert.False(serverOptions.IsDevelopmentCertificateLoaded);
    }

    [Fact]
    public async Task UseHttpsWithAsyncCallbackDoesNotFallBackToDefaultCert()
    {
        var loggerProvider = new HandshakeErrorLoggerProvider();
        LoggerFactory.AddProvider(loggerProvider);

        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(context => Task.CompletedTask,
            testContext,
            listenOptions =>
            {
                listenOptions.UseHttps((stream, clientHelloInfo, state, cancellationToken) =>
                    new ValueTask<SslServerAuthenticationOptions>(new SslServerAuthenticationOptions()), state: null);
            }))
        {
            using (var connection = server.CreateConnection())
            using (var sslStream = new SslStream(connection.Stream, true, (sender, certificate, chain, errors) => true))
            {
                var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                    sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                        enabledSslProtocols: SslProtocols.None,
                        checkCertificateRevocation: false));

                Logger.LogTrace(ex, "AuthenticateAsClientAsync Exception");
            }
        }

        var errorException = Assert.Single(loggerProvider.ErrorLogger.ErrorExceptions);
        Assert.IsType<NotSupportedException>(errorException);
    }

    [Fact]
    public void ConfigureHttpsDefaultsNeverLoadsDefaultCert()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.ConfigureHttpsDefaults(options =>
        {
            Assert.Null(options.ServerCertificate);
            options.ServerCertificate = _x509Certificate2;
            options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        });
        var ranUseHttpsAction = false;
        serverOptions.ListenLocalhost(5000, options =>
        {
            options.UseHttps(opt =>
            {
                Assert.Equal(_x509Certificate2, opt.ServerCertificate);
                Assert.Equal(ClientCertificateMode.RequireCertificate, opt.ClientCertificateMode);
                ranUseHttpsAction = true;
            });
        });

        Assert.True(ranUseHttpsAction);

        // Never lazy loaded
        Assert.False(serverOptions.IsDevelopmentCertificateLoaded);
        Assert.Null(serverOptions.DevelopmentCertificate);
    }

    [Fact]
    public void ConfigureCertSelectorNeverLoadsDefaultCert()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.ConfigureHttpsDefaults(options =>
        {
            Assert.Null(options.ServerCertificate);
            Assert.Null(options.ServerCertificateSelector);
            options.ServerCertificateSelector = (features, name) =>
            {
                return _x509Certificate2;
            };
            options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
        });
        var ranUseHttpsAction = false;
        serverOptions.ListenLocalhost(5000, options =>
        {
            options.UseHttps(opt =>
            {
                Assert.Null(opt.ServerCertificate);
                Assert.NotNull(opt.ServerCertificateSelector);
                Assert.Equal(ClientCertificateMode.RequireCertificate, opt.ClientCertificateMode);
                ranUseHttpsAction = true;
            });
        });

        Assert.True(ranUseHttpsAction);

        // Never lazy loaded
        Assert.False(serverOptions.IsDevelopmentCertificateLoaded);
        Assert.Null(serverOptions.DevelopmentCertificate);
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)] // Investigation: https://github.com/dotnet/aspnetcore/issues/22917
    public async Task EmptyRequestLoggedAsDebug()
    {
        var loggerProvider = new HandshakeErrorLoggerProvider();
        LoggerFactory.AddProvider(loggerProvider);

        await using (var server = new TestServer(context => Task.CompletedTask,
            new TestServiceContext(LoggerFactory),
            listenOptions =>
            {
                listenOptions.UseHttps(_x509Certificate2);
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
        Assert.True(loggerProvider.ErrorLogger.ErrorMessages.Count == 0,
            userMessage: string.Join(Environment.NewLine, loggerProvider.ErrorLogger.ErrorMessages));
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)] // Investigation: https://github.com/dotnet/aspnetcore/issues/22917
    public async Task ClientHandshakeFailureLoggedAsDebug()
    {
        var loggerProvider = new HandshakeErrorLoggerProvider();
        LoggerFactory.AddProvider(loggerProvider);

        await using (var server = new TestServer(context => Task.CompletedTask,
            new TestServiceContext(LoggerFactory),
            listenOptions =>
            {
                listenOptions.UseHttps(_x509Certificate2);
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
        Assert.True(loggerProvider.ErrorLogger.ErrorMessages.Count == 0,
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
                        await httpContext.Response.WriteAsync("hello, world", ct);
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
                listenOptions.UseHttps(_x509Certificate2);
            }))
        {
            using (var connection = server.CreateConnection())
            using (var sslStream = new SslStream(connection.Stream, true, (sender, certificate, chain, errors) => true))
            {
                await sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                    enabledSslProtocols: SslProtocols.None,
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
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loggerProvider = new HandshakeErrorLoggerProvider();
        loggerProvider.FilterLogger = new HttpsConnectionFilterLogger(expectedEventId: 3); // HttpConnectionEstablished
        LoggerFactory.AddProvider(loggerProvider);

        await using (var server = new TestServer(async httpContext =>
            {
                httpContext.Abort();
                try
                {
                    await httpContext.Response.WriteAsync("hello, world");
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            },
            new TestServiceContext(LoggerFactory),
            listenOptions =>
            {
                listenOptions.UseHttps(_x509Certificate2);
            }))
        {
            using (var connection = server.CreateConnection())
            using (var sslStream = new SslStream(connection.Stream, true, (sender, certificate, chain, errors) => true))
            {
                await sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                    enabledSslProtocols: SslProtocols.None,
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
                listenOptions.UseHttps(_x509Certificate2);
            }))
        {
            using (var connection = server.CreateConnection())
            using (var sslStream = new SslStream(connection.Stream, true, (sender, certificate, chain, errors) => true))
            {
                await sslStream.AuthenticateAsClientAsync("127.0.0.1", clientCertificates: null,
                    enabledSslProtocols: SslProtocols.None,
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
                listenOptions.UseHttps(_x509Certificate2);
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

        await using (var server = new TestServer(context => Task.CompletedTask,
            testContext,
            listenOptions =>
            {
                listenOptions.UseHttps(o =>
                {
                    o.ServerCertificate = new X509Certificate2(_x509Certificate2);
                    o.HandshakeTimeout = TimeSpan.FromMilliseconds(100);
                });
            }))
        {
            using (var connection = server.CreateConnection())
            {
                Assert.Equal(0, await connection.Stream.ReadAsync(new byte[1], 0, 1).DefaultTimeout());
            }
        }

        await loggerProvider.FilterLogger.LogTcs.Task.DefaultTimeout();
        Assert.Equal(2, loggerProvider.FilterLogger.LastEventId);
        Assert.Equal(LogLevel.Debug, loggerProvider.FilterLogger.LastLogLevel);
    }

    [Fact]
    public async Task HandshakeTimesOutAndIsLoggedAsDebugWithAsyncCallback()
    {
        var loggerProvider = new HandshakeErrorLoggerProvider();
        LoggerFactory.AddProvider(loggerProvider);

        var testContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(context => Task.CompletedTask,
            testContext,
            listenOptions =>
            {
                listenOptions.UseHttps(async (stream, clientHelloInfo, state, cancellationToken) =>
                {
                    await Task.Yield();

                    return new SslServerAuthenticationOptions
                    {
                        ServerCertificate = _x509Certificate2,
                    };
                }, state: null, handshakeTimeout: TimeSpan.FromMilliseconds(100));
            }))
        {
            using (var connection = server.CreateConnection())
            {
                Assert.Equal(0, await connection.Stream.ReadAsync(new byte[1], 0, 1).DefaultTimeout());
            }
        }

        await loggerProvider.FilterLogger.LogTcs.Task.DefaultTimeout();
        Assert.Equal(2, loggerProvider.FilterLogger.LastEventId);
        Assert.Equal(LogLevel.Debug, loggerProvider.FilterLogger.LastLogLevel);
    }

    [Fact]
    public async Task Http3_UseHttpsNoArgsWithDefaultCertificate_UseDefaultCertificate()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.TestOverrideDefaultCertificate = _x509Certificate2;

        IFeatureCollection bindFeatures = null;
        var multiplexedConnectionListenerFactory = new MockMultiplexedConnectionListenerFactory();
        multiplexedConnectionListenerFactory.OnBindAsync = (ep, features) =>
        {
            bindFeatures = features;
        };

        var testContext = new TestServiceContext(LoggerFactory);
        testContext.ServerOptions = serverOptions;
        await using (var server = new TestServer(context => Task.CompletedTask,
            testContext,
            serverOptions =>
            {
                serverOptions.ListenLocalhost(5001, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps();
                });
            },
            services =>
            {
                services.AddSingleton<IMultiplexedConnectionListenerFactory>(multiplexedConnectionListenerFactory);
            }))
        {
        }

        Assert.NotNull(bindFeatures);

        var sslOptions = bindFeatures.Get<TlsConnectionCallbackOptions>();
        Assert.NotNull(sslOptions);

        var sslServerAuthenticationOptions = await sslOptions.OnConnection(new TlsConnectionCallbackContext(), CancellationToken.None);
        Assert.Equal(_x509Certificate2, sslServerAuthenticationOptions.ServerCertificate);
    }

    [Fact]
    public async Task Http3_ConfigureHttpsDefaults_Works()
    {
        var serverOptions = CreateServerOptions();

        IFeatureCollection bindFeatures = null;
        var multiplexedConnectionListenerFactory = new MockMultiplexedConnectionListenerFactory();
        multiplexedConnectionListenerFactory.OnBindAsync = (ep, features) =>
        {
            bindFeatures = features;
        };

        var testContext = new TestServiceContext(LoggerFactory);
        testContext.ServerOptions = serverOptions;
        await using (var server = new TestServer(context => Task.CompletedTask,
            testContext,
            serverOptions =>
            {
                serverOptions.ConfigureHttpsDefaults(https =>
                {
                    https.ServerCertificate = _x509Certificate2;
                });
                serverOptions.ListenLocalhost(5001, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps();
                });
            },
            services =>
            {
                services.AddSingleton<IMultiplexedConnectionListenerFactory>(multiplexedConnectionListenerFactory);
            }))
        {
        }

        Assert.NotNull(bindFeatures);

        var tlsOptions = bindFeatures.Get<TlsConnectionCallbackOptions>();
        Assert.NotNull(tlsOptions);

        var sslServerAuthenticationOptions = await tlsOptions.OnConnection(new TlsConnectionCallbackContext(), CancellationToken.None);
        Assert.Equal(_x509Certificate2, sslServerAuthenticationOptions.ServerCertificate);
    }

    [Fact]
    public async Task Http1And2And3_NoUseHttps_MultiplexBindNotCalled()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.TestOverrideDefaultCertificate = _x509Certificate2;

        var bindCalled = false;
        var multiplexedConnectionListenerFactory = new MockMultiplexedConnectionListenerFactory();
        multiplexedConnectionListenerFactory.OnBindAsync = (ep, features) =>
        {
            bindCalled = true;
        };

        var testContext = new TestServiceContext(LoggerFactory);
        testContext.ServerOptions = serverOptions;
        await using (var server = new TestServer(context => Task.CompletedTask,
            testContext,
            serverOptions =>
            {
                serverOptions.ListenLocalhost(5001, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
                });
            },
            services =>
            {
                services.AddSingleton<IMultiplexedConnectionListenerFactory>(multiplexedConnectionListenerFactory);
            }))
        {
        }

        Assert.False(bindCalled);
    }

    [Fact]
    public async Task Http3_NoUseHttps_Throws()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.TestOverrideDefaultCertificate = _x509Certificate2;

        var bindCalled = false;
        var multiplexedConnectionListenerFactory = new MockMultiplexedConnectionListenerFactory();
        multiplexedConnectionListenerFactory.OnBindAsync = (ep, features) =>
        {
            bindCalled = true;
        };

        var testContext = new TestServiceContext(LoggerFactory);
        testContext.ServerOptions = serverOptions;
        var ex = await Assert.ThrowsAsync<IOException>(async () =>
        {
            await using var server = new TestServer(context => Task.CompletedTask,
                testContext,
                serverOptions =>
                {
                    serverOptions.ListenLocalhost(5001, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http3;
                    });
                },
                services =>
                {
                    services.AddSingleton<IMultiplexedConnectionListenerFactory>(multiplexedConnectionListenerFactory);
                });
        });

        Assert.False(bindCalled);
        Assert.Equal("HTTP/3 requires HTTPS.", ex.InnerException.InnerException.Message);
    }

    [Fact]
    public async Task Http3_ServerOptionsSelectionCallback_Works()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.TestOverrideDefaultCertificate = _x509Certificate2;

        IFeatureCollection bindFeatures = null;
        var multiplexedConnectionListenerFactory = new MockMultiplexedConnectionListenerFactory();
        multiplexedConnectionListenerFactory.OnBindAsync = (ep, features) =>
        {
            bindFeatures = features;
        };

        var testState = new object();
        var testContext = new TestServiceContext(LoggerFactory);
        testContext.ServerOptions = serverOptions;
        await using (var server = new TestServer(context => Task.CompletedTask,
            testContext,
            serverOptions =>
            {
                serverOptions.ListenLocalhost(5001, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps((SslStream stream, SslClientHelloInfo clientHelloInfo, object state, CancellationToken cancellationToken) =>
                    {
                        return ValueTask.FromResult(new SslServerAuthenticationOptions());
                    }, state: testState);
                });
            },
            services =>
            {
                services.AddSingleton<IMultiplexedConnectionListenerFactory>(multiplexedConnectionListenerFactory);
            }))
        {
        }

        Assert.NotNull(bindFeatures);

        var tlsOptions = bindFeatures.Get<TlsConnectionCallbackOptions>();
        Assert.NotNull(tlsOptions);

        Assert.Collection(tlsOptions.ApplicationProtocols, p => Assert.Equal(SslApplicationProtocol.Http3, p));

        Assert.Equal(testState, tlsOptions.OnConnectionState);
    }

    [Fact]
    public async Task Http3_TlsHandshakeCallbackOptions_Works()
    {
        var serverOptions = CreateServerOptions();
        serverOptions.TestOverrideDefaultCertificate = _x509Certificate2;

        IFeatureCollection bindFeatures = null;
        var multiplexedConnectionListenerFactory = new MockMultiplexedConnectionListenerFactory();
        multiplexedConnectionListenerFactory.OnBindAsync = (ep, features) =>
        {
            bindFeatures = features;
        };

        var testState = new object();
        var testContext = new TestServiceContext(LoggerFactory);
        testContext.ServerOptions = serverOptions;
        await using (var server = new TestServer(context => Task.CompletedTask,
            testContext,
            serverOptions =>
            {
                serverOptions.ListenLocalhost(5001, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http3;
                    listenOptions.UseHttps(new TlsHandshakeCallbackOptions()
                    {
                        OnConnection = context =>
                        {
                            return ValueTask.FromResult(new SslServerAuthenticationOptions());
                        },
                        OnConnectionState = testState
                    });
                });
            },
            services =>
            {
                services.AddSingleton<IMultiplexedConnectionListenerFactory>(multiplexedConnectionListenerFactory);
            }))
        {
        }

        Assert.NotNull(bindFeatures);

        var tlsOptions = bindFeatures.Get<TlsConnectionCallbackOptions>();
        Assert.Collection(tlsOptions.ApplicationProtocols, p => Assert.Equal(SslApplicationProtocol.Http3, p));

        Assert.NotNull(tlsOptions.OnConnection);
        Assert.Equal(testState, tlsOptions.OnConnectionState);
    }

    [Fact]
    public async Task ClientAttemptingToUseUnsupportedProtocolIsLoggedAsDebug()
    {
        var loggerProvider = new HandshakeErrorLoggerProvider();
        LoggerFactory.AddProvider(loggerProvider);

#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
        await using (var server = new TestServer(context => Task.CompletedTask,
            new TestServiceContext(LoggerFactory),
            listenOptions =>
            {
                listenOptions.UseHttps(TestResources.GetTestCertificate("no_extensions.pfx"), httpsOptions =>
                {
                    httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11;
                });
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
#pragma warning restore SYSLIB0039

        await loggerProvider.FilterLogger.LogTcs.Task.DefaultTimeout();
        Assert.Equal(1, loggerProvider.FilterLogger.LastEventId);
        Assert.Equal(LogLevel.Debug, loggerProvider.FilterLogger.LastLogLevel);
    }

    [Fact]
    public async Task OnAuthenticate_SeesOtherSettings()
    {
        var loggerProvider = new HandshakeErrorLoggerProvider();
        LoggerFactory.AddProvider(loggerProvider);

        var testCert = _x509Certificate2;
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
                        enabledSslProtocols: SslProtocols.None,
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

        var testCert = _x509Certificate2;
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
                        enabledSslProtocols: SslProtocols.None,
                        checkCertificateRevocation: false);
            }
        }

        Assert.True(onAuthenticateCalled, "onAuthenticateCalled");
    }

    private class HandshakeErrorLoggerProvider : ILoggerProvider
    {
        public HttpsConnectionFilterLogger FilterLogger { get; set; } = new HttpsConnectionFilterLogger();
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
        private readonly int? _expectedEventId;

        public HttpsConnectionFilterLogger()
        {
        }

        public HttpsConnectionFilterLogger(int expectedEventId)
        {
            _expectedEventId = expectedEventId;
        }

        public LogLevel LastLogLevel { get; set; }
        public EventId LastEventId { get; set; }
        public TaskCompletionSource LogTcs { get; } = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!_expectedEventId.HasValue || _expectedEventId.Value == eventId)
            {
                LastLogLevel = logLevel;
                LastEventId = eventId;
                LogTcs.SetResult();
            }
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
        public List<string> ErrorMessages => new List<string>();
        public List<Exception> ErrorExceptions { get; } = new List<Exception>();

        public bool ObjectDisposedExceptionLogged { get; set; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Error)
            {
                var log = $"Log {logLevel}[{eventId}]: {formatter(state, exception)} {exception}";
                ErrorMessages.Add(log);

                if (exception != null)
                {
                    ErrorExceptions.Add(exception);
                }
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
