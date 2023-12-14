// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class HttpsConnectionMiddlewareTests : LoggedTest
{
    private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();
    private static readonly X509Certificate2 _x509Certificate2NoExt = TestResources.GetTestCertificate("no_extensions.pfx");

    private static KestrelServerOptions CreateServerOptions()
    {
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

        var serverOptions = new KestrelServerOptions();
        serverOptions.ApplicationServices = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IHttpsConfigurationService, HttpsConfigurationService>()
            .AddSingleton<HttpsConfigurationService.IInitializer, HttpsConfigurationService.Initializer>()
            .AddSingleton(env.Object)
            .AddSingleton(new KestrelMetrics(new TestMeterFactory()))
            .BuildServiceProvider();
        return serverOptions;
    }

    [Fact]
    public async Task CanReadAndWriteWithHttpsConnectionMiddleware()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 });
        };

        await using (var server = new TestServer(App, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            var result = await server.HttpClientSlim.PostAsync($"https://localhost:{server.Port}/",
                new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("content", "Hello World?")
                }),
                validateCertificate: false);

            Assert.Equal("content=Hello+World%3F", result);
        }
    }

    [Fact]
    public async Task CanReadAndWriteWithHttpsConnectionMiddlewareWithPemCertificate()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Certificates:Default:Path"] = Path.Combine("shared", "TestCertificates", "https-aspnet.crt"),
            ["Certificates:Default:KeyPath"] = Path.Combine("shared", "TestCertificates", "https-aspnet.key"),
            ["Certificates:Default:Password"] = "aspnetcore",
        }).Build();

        var options = CreateServerOptions();

        var loader = new KestrelConfigurationLoader(options, configuration, options.ApplicationServices.GetRequiredService<IHttpsConfigurationService>(), certificatePathWatcher: null, reloadOnChange: false);
        options.ConfigurationLoader = loader; // Since we're constructing it explicitly, we have to hook it up explicitly
        loader.Load();

        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.KestrelServerOptions = options;
            listenOptions.UseHttps();
        };

        await using (var server = new TestServer(App, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            var result = await server.HttpClientSlim.PostAsync($"https://localhost:{server.Port}/",
                new FormUrlEncodedContent(new[] {
                        new KeyValuePair<string, string>("content", "Hello World?")
                }),
                validateCertificate: false);

            Assert.Equal("content=Hello+World%3F", result);
        }
    }

    [Fact]
    public async Task SslStreamIsAvailable()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 });
        };

        await using (var server = new TestServer(context =>
        {
            var feature = context.Features.Get<ISslStreamFeature>();
            Assert.NotNull(feature);
            Assert.NotNull(feature.SslStream);

            return context.Response.WriteAsync("hello world");
        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            var result = await server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
            Assert.Equal("hello world", result);
        }
    }

    [Fact]
    public async Task HandshakeDetailsAreAvailable()
    {
        string expectedHostname = null;
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(
                new HttpsConnectionAdapterOptions
                {
                    ServerCertificateSelector = (connection, name) =>
                    {
                        expectedHostname = name;
                        return _x509Certificate2;
                    }
                });
        };

        await using (var server = new TestServer(context =>
        {
            var tlsFeature = context.Features.Get<ITlsHandshakeFeature>();
            Assert.NotNull(tlsFeature);
            Assert.Equal(expectedHostname, tlsFeature.HostName);
            Assert.True(tlsFeature.Protocol > SslProtocols.None, "Protocol");
            Assert.True(tlsFeature.NegotiatedCipherSuite >= TlsCipherSuite.TLS_NULL_WITH_NULL_NULL, "NegotiatedCipherSuite");
            Assert.True(tlsFeature.CipherAlgorithm > CipherAlgorithmType.Null, "Cipher");
            Assert.True(tlsFeature.CipherStrength > 0, "CipherStrength");
            Assert.True(tlsFeature.HashAlgorithm >= HashAlgorithmType.None, "HashAlgorithm"); // May be None on Linux.
            Assert.True(tlsFeature.HashStrength >= 0, "HashStrength"); // May be 0 for some algorithms
            Assert.True(tlsFeature.KeyExchangeAlgorithm >= ExchangeAlgorithmType.None, "KeyExchangeAlgorithm"); // Maybe None on Windows 7
            Assert.True(tlsFeature.KeyExchangeStrength >= 0, "KeyExchangeStrength"); // May be 0 on mac

            return context.Response.WriteAsync("hello world");
        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            var result = await server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
            Assert.Equal("hello world", result);
        }
    }

    [Fact]
    public async Task HandshakeDetailsAreAvailableAfterAsyncCallback()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(async (stream, clientHelloInfo, state, cancellationToken) =>
            {
                await Task.Yield();

                return new SslServerAuthenticationOptions
                {
                    ServerCertificate = _x509Certificate2,
                };
            }, state: null);
        }

        await using (var server = new TestServer(context =>
        {
            var tlsFeature = context.Features.Get<ITlsHandshakeFeature>();
            Assert.NotNull(tlsFeature);
            Assert.True(tlsFeature.Protocol > SslProtocols.None, "Protocol");
            Assert.True(tlsFeature.CipherAlgorithm > CipherAlgorithmType.Null, "Cipher");
            Assert.True(tlsFeature.CipherStrength > 0, "CipherStrength");
            Assert.True(tlsFeature.HashAlgorithm >= HashAlgorithmType.None, "HashAlgorithm"); // May be None on Linux.
            Assert.True(tlsFeature.HashStrength >= 0, "HashStrength"); // May be 0 for some algorithms
            Assert.True(tlsFeature.KeyExchangeAlgorithm >= ExchangeAlgorithmType.None, "KeyExchangeAlgorithm"); // Maybe None on Windows 7
            Assert.True(tlsFeature.KeyExchangeStrength >= 0, "KeyExchangeStrength"); // May be 0 on mac

            return context.Response.WriteAsync("hello world");
        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            var result = await server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
            Assert.Equal("hello world", result);
        }
    }

    [Fact]
    public async Task RequireCertificateFailsWhenNoCertificate()
    {
        await using (var server = new TestServer(App, new TestServiceContext(LoggerFactory), listenOptions =>
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                ClientCertificateMode = ClientCertificateMode.RequireCertificate
            });
        }))
        {
            await Assert.ThrowsAnyAsync<Exception>(
                () => server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/"));
        }
    }

    [Fact]
    public async Task AllowCertificateContinuesWhenNoCertificate()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                ClientCertificateMode = ClientCertificateMode.AllowCertificate
            });
        }

        await using (var server = new TestServer(context =>
            {
                var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
                Assert.NotNull(tlsFeature);
                Assert.Null(tlsFeature.ClientCertificate);
                return context.Response.WriteAsync("hello world");
            }, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            var result = await server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
            Assert.Equal("hello world", result);
        }
    }

    [Fact]
    public async Task AsyncCallbackSettingClientCertificateRequiredContinuesWhenNoCertificate()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps((stream, clientHelloInfo, state, cancellationToken) =>
                new ValueTask<SslServerAuthenticationOptions>(new SslServerAuthenticationOptions
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateRequired = true,
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck
                }), state: null);
        }

        await using (var server = new TestServer(context =>
            {
                var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
                Assert.NotNull(tlsFeature);
                Assert.Null(tlsFeature.ClientCertificate);
                return context.Response.WriteAsync("hello world");
            }, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            var result = await server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
            Assert.Equal("hello world", result);
        }
    }

    [Fact]
    public void ThrowsWhenNoServerCertificateIsProvided()
    {
        Assert.Throws<ArgumentException>(() => CreateMiddleware(new HttpsConnectionAdapterOptions(), ListenOptions.DefaultHttpProtocols));
    }

    [Fact]
    public async Task UsesProvidedServerCertificate()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 });
        };

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStream(connection.Stream);
                await stream.AuthenticateAsClientAsync("localhost");
                Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2));
            }
        }
    }

    [Fact]
    public async Task UsesProvidedServerCertificateSelector()
    {
        var selectorCalled = 0;
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificateSelector = (connection, name) =>
                {
                    Assert.NotNull(connection);
                    Assert.NotNull(connection.Features.Get<SslStream>());
                    Assert.Equal("localhost", name);
                    selectorCalled++;
                    return _x509Certificate2;
                }
            });
        }

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStream(connection.Stream);
                await stream.AuthenticateAsClientAsync("localhost");
                Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2));
                Assert.Equal(1, selectorCalled);
            }
        }
    }

    [Fact]
    public async Task UsesProvidedAsyncCallback()
    {
        var selectorCalled = 0;
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(async (stream, clientHelloInfo, state, cancellationToken) =>
            {
                await Task.Yield();

                Assert.NotNull(stream);
                Assert.Equal("localhost", clientHelloInfo.ServerName);
                selectorCalled++;

                return new SslServerAuthenticationOptions
                {
                    ServerCertificate = _x509Certificate2
                };
            }, state: null);
        }

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStream(connection.Stream);
                await stream.AuthenticateAsClientAsync("localhost");
                Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2));
                Assert.Equal(1, selectorCalled);
            }
        }
    }

    [Fact]
    public async Task UsesProvidedServerCertificateSelectorEachTime()
    {
        var selectorCalled = 0;
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificateSelector = (connection, name) =>
                {
                    Assert.NotNull(connection);
                    Assert.NotNull(connection.Features.Get<SslStream>());
                    Assert.Equal("localhost", name);
                    selectorCalled++;
                    if (selectorCalled == 1)
                    {
                        return _x509Certificate2;
                    }
                    return _x509Certificate2NoExt;
                }
            });
        }

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStream(connection.Stream);
                await stream.AuthenticateAsClientAsync("localhost");
                Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2));
                Assert.Equal(1, selectorCalled);
            }
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStream(connection.Stream);
                await stream.AuthenticateAsClientAsync("localhost");
                Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2NoExt));
                Assert.Equal(2, selectorCalled);
            }
        }
    }

    [Fact]
    public async Task UsesProvidedServerCertificateSelectorValidatesEkus()
    {
        var selectorCalled = 0;
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificateSelector = (features, name) =>
                {
                    selectorCalled++;
                    return TestResources.GetTestCertificate("eku.code_signing.pfx");
                }
            });
        }

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStream(connection.Stream);
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                await Assert.ThrowsAsync<IOException>(() =>
                    stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false));
#pragma warning restore SYSLIB0039
                Assert.Equal(1, selectorCalled);
            }
        }
    }

    [Fact]
    public async Task UsesProvidedServerCertificateSelectorOverridesServerCertificate()
    {
        var selectorCalled = 0;
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2NoExt,
                ServerCertificateSelector = (connection, name) =>
                {
                    Assert.NotNull(connection);
                    Assert.NotNull(connection.Features.Get<SslStream>());
                    Assert.Equal("localhost", name);
                    selectorCalled++;
                    return _x509Certificate2;
                }
            });
        }

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStream(connection.Stream);
                await stream.AuthenticateAsClientAsync("localhost");
                Assert.True(stream.RemoteCertificate.Equals(_x509Certificate2));
                Assert.Equal(1, selectorCalled);
            }
        }
    }

    [Fact]
    public async Task UsesProvidedServerCertificateSelectorFailsIfYouReturnNull()
    {
        var selectorCalled = 0;
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificateSelector = (features, name) =>
                {
                    selectorCalled++;
                    return null;
                }
            });
        }

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStream(connection.Stream);
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                await Assert.ThrowsAsync<IOException>(() =>
                    stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false));
#pragma warning restore SYSLIB0039
                Assert.Equal(1, selectorCalled);
            }
        }
    }

    [Theory]
    [InlineData(HttpProtocols.Http1)]
    [InlineData(HttpProtocols.Http1AndHttp2)] // Make sure Http/1.1 doesn't regress with Http/2 enabled.
    public async Task CertificatePassedToHttpContext(HttpProtocols httpProtocols)
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.Protocols = httpProtocols;
            listenOptions.UseHttps(options =>
            {
                options.ServerCertificate = _x509Certificate2;
                options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                options.AllowAnyClientCertificate();
            });
        }

        await using (var server = new TestServer(context =>
            {
                var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
                Assert.NotNull(tlsFeature);
                Assert.NotNull(tlsFeature.ClientCertificate);
                Assert.NotNull(context.Connection.ClientCertificate);
                return context.Response.WriteAsync("hello world");
            }, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                // SslStream is used to ensure the certificate is actually passed to the server
                // HttpClient might not send the certificate because it is invalid or it doesn't match any
                // of the certificate authorities sent by the server in the SSL handshake.
                // Use a random host name to avoid the TLS session resumption cache.
                var stream = OpenSslStreamWithCert(connection.Stream);
                await stream.AuthenticateAsClientAsync(Guid.NewGuid().ToString());
                await AssertConnectionResult(stream, true);
            }
        }
    }

    [Fact]
    public async Task RenegotiateForClientCertificateOnHttp1DisabledByDefault()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.Protocols = HttpProtocols.Http1;
            listenOptions.UseHttps(options =>
            {
                options.ServerCertificate = _x509Certificate2;
                options.ClientCertificateMode = ClientCertificateMode.NoCertificate;
                options.AllowAnyClientCertificate();
            });
        }

        await using var server = new TestServer(async context =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tlsFeature);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            var clientCert = await context.Connection.GetClientCertificateAsync();
            Assert.Null(clientCert);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            await context.Response.WriteAsync("hello world");
        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        // SslStream is used to ensure the certificate is actually passed to the server
        // HttpClient might not send the certificate because it is invalid or it doesn't match any
        // of the certificate authorities sent by the server in the SSL handshake.
        // Use a random host name to avoid the TLS session resumption cache.
        var stream = OpenSslStreamWithCert(connection.Stream);
        await stream.AuthenticateAsClientAsync(Guid.NewGuid().ToString());
        await AssertConnectionResult(stream, true);
    }

    [ConditionalTheory]
    [InlineData(HttpProtocols.Http1)]
    [InlineData(HttpProtocols.Http1AndHttp2)] // Make sure turning on Http/2 doesn't regress HTTP/1
    [TlsAlpnSupported]
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing platform support.")]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/33566#issuecomment-892031659", Queues = HelixConstants.AlmaLinuxAmd64)] // Outdated OpenSSL client
    public async Task CanRenegotiateForClientCertificate(HttpProtocols httpProtocols)
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.Protocols = httpProtocols;
            listenOptions.UseHttps(options =>
            {
                options.ServerCertificate = _x509Certificate2;
                options.ClientCertificateMode = ClientCertificateMode.DelayCertificate;
                options.AllowAnyClientCertificate();
            });
        }

        await using var server = new TestServer(async context =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tlsFeature);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            var clientCert = await context.Connection.GetClientCertificateAsync();
            Assert.NotNull(clientCert);
            Assert.NotNull(tlsFeature.ClientCertificate);
            Assert.NotNull(context.Connection.ClientCertificate);

            await context.Response.WriteAsync("hello world");
        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        // SslStream is used to ensure the certificate is actually passed to the server
        // HttpClient might not send the certificate because it is invalid or it doesn't match any
        // of the certificate authorities sent by the server in the SSL handshake.
        // Use a random host name to avoid the TLS session resumption cache.
        var stream = OpenSslStreamWithCert(connection.Stream);
        await stream.AuthenticateAsClientAsync(Guid.NewGuid().ToString());
        await AssertConnectionResult(stream, true);
    }

    [ConditionalFact]
    [TlsAlpnSupported]
    [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.Linux, SkipReason = "MacOS only test.")]
    public async Task CanRenegotiateForClientCertificate_MacOS_PlatformNotSupportedException()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.Protocols = HttpProtocols.Http1;
            listenOptions.UseHttps(options =>
            {
                options.ServerCertificate = _x509Certificate2;
                options.ClientCertificateMode = ClientCertificateMode.DelayCertificate;
                options.AllowAnyClientCertificate();
            });
        }

        await using var server = new TestServer(async context =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tlsFeature);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            await Assert.ThrowsAsync<PlatformNotSupportedException>(() => context.Connection.GetClientCertificateAsync());

            var lifetimeNotificationFeature = context.Features.Get<IConnectionLifetimeNotificationFeature>();
            Assert.False(
                lifetimeNotificationFeature.ConnectionClosedRequested.IsCancellationRequested,
                "GetClientCertificateAsync shouldn't cause the connection to be closed.");

            await context.Response.WriteAsync("hello world");
        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        // SslStream is used to ensure the certificate is actually passed to the server
        // HttpClient might not send the certificate because it is invalid or it doesn't match any
        // of the certificate authorities sent by the server in the SSL handshake.
        // Use a random host name to avoid the TLS session resumption cache.
        var stream = OpenSslStreamWithCert(connection.Stream);
        await stream.AuthenticateAsClientAsync(Guid.NewGuid().ToString());
        await AssertConnectionResult(stream, true);
    }

    [Fact]
    public async Task Renegotiate_ServerOptionsSelectionCallback_NotSupported()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps((SslStream stream, SslClientHelloInfo clientHelloInfo, object state, CancellationToken cancellationToken) =>
            {
                return ValueTask.FromResult(new SslServerAuthenticationOptions()
                {
                    ServerCertificate = _x509Certificate2,
                    ClientCertificateRequired = false,
                    RemoteCertificateValidationCallback = (_, _, _, _) => true,
                });
            }, state: null);
        }

        await using var server = new TestServer(async context =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tlsFeature);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            var clientCert = await context.Connection.GetClientCertificateAsync();
            Assert.Null(clientCert);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            await context.Response.WriteAsync("hello world");
        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        // SslStream is used to ensure the certificate is actually passed to the server
        // HttpClient might not send the certificate because it is invalid or it doesn't match any
        // of the certificate authorities sent by the server in the SSL handshake.
        // Use a random host name to avoid the TLS session resumption cache.
        var stream = OpenSslStreamWithCert(connection.Stream);
        await stream.AuthenticateAsClientAsync(Guid.NewGuid().ToString());
        await AssertConnectionResult(stream, true);
    }

    [ConditionalFact]
    [TlsAlpnSupported]
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing platform support.")]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/33566#issuecomment-892031659", Queues = HelixConstants.AlmaLinuxAmd64)] // Outdated OpenSSL client
    public async Task CanRenegotiateForTlsCallbackOptions()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new TlsHandshakeCallbackOptions()
            {
                OnConnection = context =>
                {
                    context.AllowDelayedClientCertificateNegotation = true;
                    return ValueTask.FromResult(new SslServerAuthenticationOptions()
                    {
                        ServerCertificate = _x509Certificate2,
                        ClientCertificateRequired = false,
                        RemoteCertificateValidationCallback = (_, _, _, _) => true,
                    });
                }
            });
        }

        await using var server = new TestServer(async context =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tlsFeature);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            var clientCert = await context.Connection.GetClientCertificateAsync();
            Assert.NotNull(clientCert);
            Assert.NotNull(tlsFeature.ClientCertificate);
            Assert.NotNull(context.Connection.ClientCertificate);

            await context.Response.WriteAsync("hello world");
        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        // SslStream is used to ensure the certificate is actually passed to the server
        // HttpClient might not send the certificate because it is invalid or it doesn't match any
        // of the certificate authorities sent by the server in the SSL handshake.
        // Use a random host name to avoid the TLS session resumption cache.
        var stream = OpenSslStreamWithCert(connection.Stream);
        await stream.AuthenticateAsClientAsync(Guid.NewGuid().ToString());
        await AssertConnectionResult(stream, true);
    }

    [ConditionalFact]
    [TlsAlpnSupported]
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing platform support.")]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/33566#issuecomment-892031659", Queues = HelixConstants.AlmaLinuxAmd64)] // Outdated OpenSSL client
    public async Task CanRenegotiateForClientCertificateOnHttp1CanReturnNoCert()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.Protocols = HttpProtocols.Http1;
            listenOptions.UseHttps(options =>
            {
                options.ServerCertificate = _x509Certificate2;
                options.ClientCertificateMode = ClientCertificateMode.DelayCertificate;
                options.AllowAnyClientCertificate();
            });
        }

        await using var server = new TestServer(async context =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tlsFeature);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            var clientCert = await context.Connection.GetClientCertificateAsync();
            Assert.Null(clientCert);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            await context.Response.WriteAsync("hello world");
        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        // SslStream is used to ensure the certificate is actually passed to the server
        // HttpClient might not send the certificate because it is invalid or it doesn't match any
        // of the certificate authorities sent by the server in the SSL handshake.
        // Use a random host name to avoid the TLS session resumption cache.
        var stream = new SslStream(connection.Stream);
        var clientOptions = new SslClientAuthenticationOptions()
        {
            TargetHost = Guid.NewGuid().ToString(),
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
            EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
#pragma warning restore SYSLIB0039
        };
        clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

        await stream.AuthenticateAsClientAsync(clientOptions);
        await AssertConnectionResult(stream, true);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Fails on OSX.")]
    public async Task ServerCertificateChainInExtraStore()
    {
        var streams = new List<SslStream>();
        CertHelper.CleanupCertificates(nameof(ServerCertificateChainInExtraStore));
        (var clientCertificate, var clientChain) = CertHelper.GenerateCertificates(nameof(ServerCertificateChainInExtraStore), longChain: true, serverCertificate: false);

        using (var store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
        {
            // add chain certificate so we can construct chain since there is no way how to pass intermediates directly.
            store.Open(OpenFlags.ReadWrite);
            store.AddRange(clientChain);
            store.Close();
        }

        using (var chain = new X509Chain())
        {
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.DisableCertificateDownloads = true;
            var chainStatus = chain.Build(clientCertificate);
        }

        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                ServerCertificateChain = clientChain,
                OnAuthenticate = (con, so) =>
                {
                    so.ClientCertificateRequired = true;
                    so.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        Assert.NotEmpty(chain.ChainPolicy.ExtraStore);
                        Assert.Contains(clientChain[0], chain.ChainPolicy.ExtraStore);
                        return true;
                    };
                    so.CertificateRevocationCheckMode = X509RevocationMode.NoCheck;
                }
            });
        }

        await using (var server = new TestServer(
            context => context.Response.WriteAsync("hello world"),
            new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStreamWithCert(connection.Stream, clientCertificate);
                await stream.AuthenticateAsClientAsync("localhost");
                await AssertConnectionResult(stream, true);
            }
        }

        CertHelper.CleanupCertificates(nameof(ServerCertificateChainInExtraStore));
        clientCertificate.Dispose();
        var list = (System.Collections.IList)clientChain;
        for (var i = 0; i < list.Count; i++)
        {
            var c = (X509Certificate)list[i];
            c.Dispose();
        }

        foreach (var s in streams)
        {
            s.Dispose();
        }
    }

    [ConditionalFact]
    // TLS 1.2 and lower have to renegotiate the whole connection to get a client cert, and if that hits an error
    // then the connection is aborted.
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing platform support.")]
    [TlsAlpnSupported]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/33566#issuecomment-892031659", Queues = HelixConstants.AlmaLinuxAmd64)] // Outdated OpenSSL client
    public async Task RenegotiateForClientCertificateOnPostWithoutBufferingThrows()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.Protocols = HttpProtocols.Http1;
            listenOptions.UseHttps(options =>
            {
                options.ServerCertificate = _x509Certificate2;
                options.ClientCertificateMode = ClientCertificateMode.DelayCertificate;
                options.AllowAnyClientCertificate();
            });
        }

        // Under 4kb can sometimes work because it fits into Kestrel's header parsing buffer.
        var expectedBody = new string('a', 1024 * 4);

        await using var server = new TestServer(async context =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tlsFeature);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Connection.GetClientCertificateAsync());
            Assert.Equal("Client stream needs to be drained before renegotiation.", ex.Message);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);
        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        // SslStream is used to ensure the certificate is actually passed to the server
        // HttpClient might not send the certificate because it is invalid or it doesn't match any
        // of the certificate authorities sent by the server in the SSL handshake.
        // Use a random host name to avoid the TLS session resumption cache.
        var stream = OpenSslStreamWithCert(connection.Stream);
        await stream.AuthenticateAsClientAsync(Guid.NewGuid().ToString());
        await AssertConnectionResult(stream, true, expectedBody);
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)] // HTTP/2 requires Win10
    [TlsAlpnSupported]
    public async Task ServerOptionsSelectionCallback_SetsALPN()
    {
        static void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps((_, _, _, _) =>
                ValueTask.FromResult(new SslServerAuthenticationOptions()
                {
                    ServerCertificate = _x509Certificate2,
                }), state: null);
        }

        await using var server = new TestServer(context => Task.CompletedTask,
            new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        var stream = OpenSslStream(connection.Stream);
        await stream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions()
        {
            // Use a random host name to avoid the TLS session resumption cache.
            TargetHost = Guid.NewGuid().ToString(),
            ApplicationProtocols = new() { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11, },
        });
        Assert.Equal(SslApplicationProtocol.Http2, stream.NegotiatedApplicationProtocol);
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)] // HTTP/2 requires Win10
    [TlsAlpnSupported]
    public async Task TlsHandshakeCallbackOptionsOverload_SetsALPN()
    {
        static void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new TlsHandshakeCallbackOptions()
            {
                OnConnection = context =>
                {
                    return ValueTask.FromResult(new SslServerAuthenticationOptions()
                    {
                        ServerCertificate = _x509Certificate2,
                    });
                }
            });
        }

        await using var server = new TestServer(context => Task.CompletedTask,
            new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        var stream = OpenSslStream(connection.Stream);
        await stream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions()
        {
            // Use a random host name to avoid the TLS session resumption cache.
            TargetHost = Guid.NewGuid().ToString(),
            ApplicationProtocols = new() { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11, },
        });
        Assert.Equal(SslApplicationProtocol.Http2, stream.NegotiatedApplicationProtocol);
    }

    [ConditionalFact]
    [TlsAlpnSupported]
    public async Task TlsHandshakeCallbackOptionsOverload_EmptyAlpnList_DisablesAlpn()
    {
        static void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new TlsHandshakeCallbackOptions()
            {
                OnConnection = context =>
                {
                    return ValueTask.FromResult(new SslServerAuthenticationOptions()
                    {
                        ServerCertificate = _x509Certificate2,
                        ApplicationProtocols = new(),
                    });
                }
            });
        }

        await using var server = new TestServer(context => Task.CompletedTask,
            new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        var stream = OpenSslStream(connection.Stream);
        await stream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions()
        {
            // Use a random host name to avoid the TLS session resumption cache.
            TargetHost = Guid.NewGuid().ToString(),
            ApplicationProtocols = new() { SslApplicationProtocol.Http2, SslApplicationProtocol.Http11, },
        });
        Assert.Equal(default, stream.NegotiatedApplicationProtocol);
    }

    [ConditionalFact]
    [TlsAlpnSupported]
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing platform support.")]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/33566#issuecomment-892031659", Queues = HelixConstants.AlmaLinuxAmd64)] // Outdated OpenSSL client
    public async Task CanRenegotiateForClientCertificateOnPostIfDrained()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.Protocols = HttpProtocols.Http1;
            listenOptions.UseHttps(options =>
            {
                options.ServerCertificate = _x509Certificate2;
                options.ClientCertificateMode = ClientCertificateMode.DelayCertificate;
                options.AllowAnyClientCertificate();
            });
        }

        var expectedBody = new string('a', 1024 * 4);

        await using var server = new TestServer(async context =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tlsFeature);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            // Read the body before requesting the client cert
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            Assert.Equal(expectedBody, body);

            var clientCert = await context.Connection.GetClientCertificateAsync();
            Assert.NotNull(clientCert);
            Assert.NotNull(tlsFeature.ClientCertificate);
            Assert.NotNull(context.Connection.ClientCertificate);
            await context.Response.WriteAsync("hello world");
        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        // SslStream is used to ensure the certificate is actually passed to the server
        // HttpClient might not send the certificate because it is invalid or it doesn't match any
        // of the certificate authorities sent by the server in the SSL handshake.
        // Use a random host name to avoid the TLS session resumption cache.
        var stream = OpenSslStreamWithCert(connection.Stream);
        await stream.AuthenticateAsClientAsync(Guid.NewGuid().ToString());
        await AssertConnectionResult(stream, true, expectedBody);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing platform support.")]
    [TlsAlpnSupported]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/33566#issuecomment-892031659", Queues = HelixConstants.AlmaLinuxAmd64)] // Outdated OpenSSL client
    public async Task RenegotationFailureCausesConnectionClose()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.Protocols = HttpProtocols.Http1;
            listenOptions.UseHttps(options =>
            {
                options.ServerCertificate = _x509Certificate2;
                options.ClientCertificateMode = ClientCertificateMode.DelayCertificate;
                options.AllowAnyClientCertificate();
            });
        }

        var expectedBody = new string('a', 1024 * 4);

        await using var server = new TestServer(async context =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tlsFeature);
            Assert.Null(tlsFeature.ClientCertificate);
            Assert.Null(context.Connection.ClientCertificate);

            // Request the client cert while there's still body data in the buffers
            var ioe = await Assert.ThrowsAsync<InvalidOperationException>(() => context.Connection.GetClientCertificateAsync());
            Assert.Equal("Client stream needs to be drained before renegotiation.", ioe.Message);

            context.Response.ContentLength = 11;
            await context.Response.WriteAsync("hello world");

        }, new TestServiceContext(LoggerFactory), ConfigureListenOptions);

        using var connection = server.CreateConnection();
        // SslStream is used to ensure the certificate is actually passed to the server
        // HttpClient might not send the certificate because it is invalid or it doesn't match any
        // of the certificate authorities sent by the server in the SSL handshake.
        // Use a random host name to avoid the TLS session resumption cache.
        var stream = OpenSslStreamWithCert(connection.Stream);
        await stream.AuthenticateAsClientAsync(Guid.NewGuid().ToString());

        var request = Encoding.UTF8.GetBytes($"POST / HTTP/1.1\r\nHost: localhost\r\nContent-Length: {expectedBody.Length}\r\n\r\n{expectedBody}");
        await stream.WriteAsync(request, 0, request.Length).DefaultTimeout();
        var reader = new StreamReader(stream);
        Assert.Equal("HTTP/1.1 200 OK", await reader.ReadLineAsync().DefaultTimeout());
        Assert.Equal("Content-Length: 11", await reader.ReadLineAsync().DefaultTimeout());
        Assert.Equal("Connection: close", await reader.ReadLineAsync().DefaultTimeout());
        Assert.StartsWith("Date: ", await reader.ReadLineAsync().DefaultTimeout());
        Assert.Equal("", await reader.ReadLineAsync().DefaultTimeout());
        Assert.Equal("hello world", await reader.ReadLineAsync().DefaultTimeout());
        Assert.Null(await reader.ReadLineAsync().DefaultTimeout());
    }

    [Fact]
    public async Task HttpsSchemePassedToRequestFeature()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 });
        }

        await using (var server = new TestServer(context => context.Response.WriteAsync(context.Request.Scheme), new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            var result = await server.HttpClientSlim.GetStringAsync($"https://localhost:{server.Port}/", validateCertificate: false);
            Assert.Equal("https", result);
        }
    }

    [Fact]
    public async Task Tls10CanBeDisabled()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(options =>
            {
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                options.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11;
#pragma warning restore SYSLIB0039
                options.ServerCertificate = _x509Certificate2;
                options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                options.AllowAnyClientCertificate();
            });
        }

        await using (var server = new TestServer(context => context.Response.WriteAsync("hello world"), new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            // SslStream is used to ensure the certificate is actually passed to the server
            // HttpClient might not send the certificate because it is invalid or it doesn't match any
            // of the certificate authorities sent by the server in the SSL handshake.
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStreamWithCert(connection.Stream);
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                var ex = await Assert.ThrowsAnyAsync<Exception>(
                    async () => await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls, false));
#pragma warning restore SYSLIB0039
            }
        }
    }

    [Theory]
    [InlineData(ClientCertificateMode.AllowCertificate)]
    [InlineData(ClientCertificateMode.RequireCertificate)]
    public async Task ClientCertificateValidationGetsCalledWithNotNullParameters(ClientCertificateMode mode)
    {
        var clientCertificateValidationCalled = false;
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                ClientCertificateMode = mode,
                ClientCertificateValidation = (certificate, chain, sslPolicyErrors) =>
                {
                    clientCertificateValidationCalled = true;
                    Assert.NotNull(certificate);
                    Assert.NotNull(chain);
                    return true;
                }
            });
        }

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStreamWithCert(connection.Stream);
                await stream.AuthenticateAsClientAsync("localhost");
                await AssertConnectionResult(stream, true);
                Assert.True(clientCertificateValidationCalled);
            }
        }
    }

    [ConditionalTheory]
    [InlineData(ClientCertificateMode.AllowCertificate)]
    [InlineData(ClientCertificateMode.RequireCertificate)]
    public async Task ValidationFailureRejectsConnection(ClientCertificateMode mode)
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                ClientCertificateMode = mode,
                ClientCertificateValidation = (certificate, chain, sslPolicyErrors) => false
            });
        }

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStreamWithCert(connection.Stream);
                await stream.AuthenticateAsClientAsync("localhost");
                await AssertConnectionResult(stream, false);
            }
        }
    }

    [Theory]
    [InlineData(ClientCertificateMode.AllowCertificate)]
    [InlineData(ClientCertificateMode.RequireCertificate)]
    public async Task RejectsConnectionOnSslPolicyErrorsWhenNoValidation(ClientCertificateMode mode)
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                ClientCertificateMode = mode
            });
        }

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStreamWithCert(connection.Stream);
                await stream.AuthenticateAsClientAsync("localhost");
                await AssertConnectionResult(stream, false);
            }
        }
    }

    [Fact]
    public async Task AllowAnyCertOverridesCertificateValidation()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(options =>
            {
                options.ServerCertificate = _x509Certificate2;
                options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                options.ClientCertificateValidation = (certificate, x509Chain, sslPolicyErrors) => false;
                options.AllowAnyClientCertificate();
            });
        }

        await using (var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStreamWithCert(connection.Stream);
                await stream.AuthenticateAsClientAsync("localhost");
                await AssertConnectionResult(stream, true);
            }
        }
    }

    [Fact]
    public async Task CertificatePassedToHttpContextIsNotDisposed()
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(options =>
            {
                options.ServerCertificate = _x509Certificate2;
                options.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                options.AllowAnyClientCertificate();
            });
        }

        RequestDelegate app = context =>
        {
            var tlsFeature = context.Features.Get<ITlsConnectionFeature>();
            Assert.NotNull(tlsFeature);
            Assert.NotNull(tlsFeature.ClientCertificate);
            Assert.NotNull(context.Connection.ClientCertificate);
            Assert.NotNull(context.Connection.ClientCertificate.PublicKey);
            return context.Response.WriteAsync("hello world");
        };

        await using (var server = new TestServer(app, new TestServiceContext(LoggerFactory), ConfigureListenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                var stream = OpenSslStreamWithCert(connection.Stream);
                await stream.AuthenticateAsClientAsync("localhost");
                await AssertConnectionResult(stream, true);
            }
        }
    }

    [Theory]
    [InlineData("no_extensions.pfx")]
    public void AcceptsCertificateWithoutExtensions(string testCertName)
    {
        var certPath = TestResources.GetCertPath(testCertName);
        TestOutputHelper.WriteLine("Loading " + certPath);
        var cert = new X509Certificate2(certPath, "testPassword");
        Assert.Empty(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());

        CreateMiddleware(cert);
    }

    [Theory]
    [InlineData("eku.server.pfx")]
    [InlineData("eku.multiple_usages.pfx")]
    public void ValidatesEnhancedKeyUsageOnCertificate(string testCertName)
    {
        var certPath = TestResources.GetCertPath(testCertName);
        TestOutputHelper.WriteLine("Loading " + certPath);
        var cert = new X509Certificate2(certPath, "testPassword");
        Assert.NotEmpty(cert.Extensions);
        var eku = Assert.Single(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());
        Assert.NotEmpty(eku.EnhancedKeyUsages);

        CreateMiddleware(new HttpsConnectionAdapterOptions
        {
            ServerCertificate = cert,
        },
        ListenOptions.DefaultHttpProtocols);
    }

    [Theory]
    [InlineData("eku.code_signing.pfx")]
    [InlineData("eku.client.pfx")]
    public void ThrowsForCertificatesMissingServerEku(string testCertName)
    {
        var certPath = TestResources.GetCertPath(testCertName);
        TestOutputHelper.WriteLine("Loading " + certPath);
        var cert = new X509Certificate2(certPath, "testPassword");
        Assert.NotEmpty(cert.Extensions);
        var eku = Assert.Single(cert.Extensions.OfType<X509EnhancedKeyUsageExtension>());
        Assert.NotEmpty(eku.EnhancedKeyUsages);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            CreateMiddleware(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = cert,
            },
            ListenOptions.DefaultHttpProtocols));

        Assert.Equal(CoreStrings.FormatInvalidServerCertificateEku(cert.Thumbprint), ex.Message);
    }

    [Theory]
    [InlineData("no_extensions.pfx")]
    public void LogsForCertificateMissingSubjectAlternativeName(string testCertName)
    {
        var certPath = TestResources.GetCertPath(testCertName);
        TestOutputHelper.WriteLine("Loading " + certPath);
        var cert = new X509Certificate2(certPath, "testPassword");
        Assert.False(CertificateLoader.DoesCertificateHaveASubjectAlternativeName(cert));

        var testLogger = new TestApplicationErrorLogger();
        CreateMiddleware(
            new HttpsConnectionAdapterOptions
            {
                ServerCertificate = cert,
            },
            ListenOptions.DefaultHttpProtocols,
            testLogger);

        Assert.Single(testLogger.Messages.Where(log => log.EventId == 9));
    }

    [ConditionalTheory]
    [InlineData(HttpProtocols.Http1)]
    [InlineData(HttpProtocols.Http2)]
    [InlineData(HttpProtocols.Http1AndHttp2)]
    [TlsAlpnSupported]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
    public async Task ListenOptionsProtolsCanBeSetAfterUseHttps(HttpProtocols httpProtocols)
    {
        void ConfigureListenOptions(ListenOptions listenOptions)
        {
            listenOptions.UseHttps(_x509Certificate2);
            listenOptions.Protocols = httpProtocols;
        }

        await using var server = new TestServer(context => Task.CompletedTask, new TestServiceContext(LoggerFactory), ConfigureListenOptions);
        using var connection = server.CreateConnection();

        var sslOptions = new SslClientAuthenticationOptions
        {
            TargetHost = "localhost",
            EnabledSslProtocols = SslProtocols.None,
            ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http11, SslApplicationProtocol.Http2 },
        };

        using var stream = OpenSslStream(connection.Stream);
        await stream.AuthenticateAsClientAsync(sslOptions);

        Assert.Equal(
            httpProtocols.HasFlag(HttpProtocols.Http2) ?
                SslApplicationProtocol.Http2 :
                SslApplicationProtocol.Http11,
            stream.NegotiatedApplicationProtocol);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Downgrade logic only applies on Windows")]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81)]
    public void Http1AndHttp2DowngradeToHttp1ForHttpsOnIncompatibleWindowsVersions()
    {
        var httpConnectionAdapterOptions = new HttpsConnectionAdapterOptions
        {
            ServerCertificate = _x509Certificate2,
        };
        var middleware = CreateMiddleware(httpConnectionAdapterOptions, HttpProtocols.Http1AndHttp2);

        Assert.Equal(HttpProtocols.Http1, middleware._httpProtocols);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Downgrade logic only applies on Windows")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
    public void Http1AndHttp2DoesNotDowngradeOnCompatibleWindowsVersions()
    {
        var httpConnectionAdapterOptions = new HttpsConnectionAdapterOptions
        {
            ServerCertificate = _x509Certificate2,
        };
        var middleware = CreateMiddleware(httpConnectionAdapterOptions, HttpProtocols.Http1AndHttp2);

        Assert.Equal(HttpProtocols.Http1AndHttp2, middleware._httpProtocols);
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Error logic only applies on Windows")]
    [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81)]
    public void Http2ThrowsOnIncompatibleWindowsVersions()
    {
        var httpConnectionAdapterOptions = new HttpsConnectionAdapterOptions
        {
            ServerCertificate = _x509Certificate2,
        };

        Assert.Throws<NotSupportedException>(() => CreateMiddleware(httpConnectionAdapterOptions, HttpProtocols.Http2));
    }

    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Error logic only applies on Windows")]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
    public void Http2DoesNotThrowOnCompatibleWindowsVersions()
    {
        var httpConnectionAdapterOptions = new HttpsConnectionAdapterOptions
        {
            ServerCertificate = _x509Certificate2,
        };

        // Does not throw
        CreateMiddleware(httpConnectionAdapterOptions, HttpProtocols.Http2);
    }

    private static HttpsConnectionMiddleware CreateMiddleware(X509Certificate2 serverCertificate)
    {
        return CreateMiddleware(new HttpsConnectionAdapterOptions
        {
            ServerCertificate = serverCertificate,
        },
        ListenOptions.DefaultHttpProtocols);
    }

    private static HttpsConnectionMiddleware CreateMiddleware(HttpsConnectionAdapterOptions options, HttpProtocols httpProtocols, TestApplicationErrorLogger testLogger = null)
    {
        var loggerFactory = testLogger is null ? (ILoggerFactory)NullLoggerFactory.Instance : new LoggerFactory(new[] { new KestrelTestLoggerProvider(testLogger) });
        return new HttpsConnectionMiddleware(context => Task.CompletedTask, options, httpProtocols, loggerFactory, new KestrelMetrics(new TestMeterFactory()));
    }

    private static HttpsConnectionMiddleware CreateMiddleware(HttpsConnectionAdapterOptions options, HttpProtocols httpProtocols)
    {
        return new HttpsConnectionMiddleware(context => Task.CompletedTask, options, httpProtocols, new KestrelMetrics(new TestMeterFactory()));
    }

    private static async Task App(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var response = httpContext.Response;
        while (true)
        {
            var buffer = new byte[8192];
            var count = await request.Body.ReadAsync(buffer, 0, buffer.Length);
            if (count == 0)
            {
                break;
            }
            await response.Body.WriteAsync(buffer, 0, count);
        }
    }

    private static SslStream OpenSslStream(Stream rawStream)
    {
        return new SslStream(rawStream, false, (sender, certificate, chain, errors) => true);
    }

    /// <summary>
    /// SslStream is used to ensure the certificate is actually passed to the server
    /// HttpClient might not send the certificate because it is invalid or it doesn't match any
    /// of the certificate authorities sent by the server in the SSL handshake.
    /// </summary>
    private static SslStream OpenSslStreamWithCert(Stream rawStream, X509Certificate2 clientCertificate = null)
    {
        return new SslStream(rawStream, false, (sender, certificate, chain, errors) => true,
            (sender, host, certificates, certificate, issuers) => clientCertificate ?? _x509Certificate2);
    }

    private static async Task AssertConnectionResult(SslStream stream, bool success, string body = null)
    {
        var request = body == null ? Encoding.UTF8.GetBytes("GET / HTTP/1.0\r\n\r\n")
            : Encoding.UTF8.GetBytes($"POST / HTTP/1.0\r\nContent-Length: {body.Length}\r\n\r\n{body}");
        await stream.WriteAsync(request, 0, request.Length);
        var reader = new StreamReader(stream);
        string line = null;
        if (success)
        {
            line = await reader.ReadLineAsync();
            Assert.Equal("HTTP/1.1 200 OK", line);
        }
        else
        {
            try
            {
                line = await reader.ReadLineAsync();
            }
            catch (IOException) { }
            Assert.Null(line);
        }
    }
}
