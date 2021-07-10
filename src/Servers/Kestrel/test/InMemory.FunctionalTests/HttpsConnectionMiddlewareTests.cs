// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class HttpsConnectionMiddlewareTests : LoggedTest
    {
        private static readonly X509Certificate2 _x509Certificate2 = TestResources.GetTestCertificate();
        private static readonly X509Certificate2 _x509Certificate2NoExt = TestResources.GetTestCertificate("no_extensions.pfx");

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

            var options = new KestrelServerOptions();
            var env = new Mock<IHostEnvironment>();
            env.SetupGet(e => e.ContentRootPath).Returns(Directory.GetCurrentDirectory());

            var serviceProvider = new ServiceCollection().AddLogging().BuildServiceProvider();
            options.ApplicationServices = serviceProvider;

            var logger = serviceProvider.GetRequiredService<ILogger<KestrelServer>>();
            var httpsLogger = serviceProvider.GetRequiredService<ILogger<HttpsConnectionMiddleware>>();
            var loader = new KestrelConfigurationLoader(options, configuration, env.Object, reloadOnChange: false, logger, httpsLogger);
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
        public async Task HandshakeDetailsAreAvailable()
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.UseHttps(new HttpsConnectionAdapterOptions { ServerCertificate = _x509Certificate2 });
            };

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
            var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
            listenOptions.UseHttps(new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                ClientCertificateMode = ClientCertificateMode.RequireCertificate
            });

            await using (var server = new TestServer(App, new TestServiceContext(LoggerFactory), listenOptions))
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
            Assert.Throws<ArgumentException>(() => new HttpsConnectionMiddleware(context => Task.CompletedTask,
                new HttpsConnectionAdapterOptions())
                );
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
                    await Assert.ThrowsAsync<IOException>(() =>
                        stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false));
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
                    await Assert.ThrowsAsync<IOException>(() =>
                        stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls12 | SslProtocols.Tls11, false));
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
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Not supported yet.")]
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
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Not supported yet.")]
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
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Not supported yet.")]
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
                EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12,
            };
            clientOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

            await stream.AuthenticateAsClientAsync(clientOptions);
            await AssertConnectionResult(stream, true);
        }

        [ConditionalFact]
        // TLS 1.2 and lower have to renegotiate the whole connection to get a client cert, and if that hits an error
        // then the connection is aborted.
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Not supported yet.")]
        public async Task RenegotiateForClientCertificateOnPostWithoutBufferingThrows_TLS12()
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.Protocols = HttpProtocols.Http1;
                listenOptions.UseHttps(options =>
                {
                    options.SslProtocols = SslProtocols.Tls12;
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
                Assert.Equal("Received data during renegotiation.", ex.Message);
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
            await AssertConnectionResult(stream, false, expectedBody);
        }

        [ConditionalFact]
        // TLS 1.3 uses a new client cert negotiation extension that doesn't cause the connection to abort
        // for this error.
        [MinimumOSVersion(OperatingSystems.Windows, "10.0.20145")] // Needs a preview version with TLS 1.3 enabled.
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Not supported yet.")]
        public async Task RenegotiateForClientCertificateOnPostWithoutBufferingThrows_TLS13()
        {
            void ConfigureListenOptions(ListenOptions listenOptions)
            {
                listenOptions.Protocols = HttpProtocols.Http1;
                listenOptions.UseHttps(options =>
                {
                    options.SslProtocols = SslProtocols.Tls13;
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
                Assert.Equal("Received data during renegotiation.", ex.Message);
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
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "ALPN not supported")]
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
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "ALPN not supported")]
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
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "ALPN not supported")]
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
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Not supported yet.")]
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
                    options.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11;
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
                    var ex = await Assert.ThrowsAnyAsync<Exception>(
                        async () => await stream.AuthenticateAsClientAsync("localhost", new X509CertificateCollection(), SslProtocols.Tls, false));
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

            new HttpsConnectionMiddleware(context => Task.CompletedTask, new HttpsConnectionAdapterOptions
            {
                ServerCertificate = cert,
            });
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

            new HttpsConnectionMiddleware(context => Task.CompletedTask, new HttpsConnectionAdapterOptions
            {
                ServerCertificate = cert,
            });
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
                new HttpsConnectionMiddleware(context => Task.CompletedTask, new HttpsConnectionAdapterOptions
                {
                    ServerCertificate = cert,
                }));

            Assert.Equal(CoreStrings.FormatInvalidServerCertificateEku(cert.Thumbprint), ex.Message);
        }

        [ConditionalTheory]
        [InlineData(HttpProtocols.Http1)]
        [InlineData(HttpProtocols.Http2)]
        [InlineData(HttpProtocols.Http1AndHttp2)]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Missing SslStream ALPN support: https://github.com/dotnet/runtime/issues/27727")]
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
                HttpProtocols = HttpProtocols.Http1AndHttp2
            };
            new HttpsConnectionMiddleware(context => Task.CompletedTask, httpConnectionAdapterOptions);

            Assert.Equal(HttpProtocols.Http1, httpConnectionAdapterOptions.HttpProtocols);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Downgrade logic only applies on Windows")]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
        public void Http1AndHttp2DoesNotDowngradeOnCompatibleWindowsVersions()
        {
            var httpConnectionAdapterOptions = new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                HttpProtocols = HttpProtocols.Http1AndHttp2
            };
            new HttpsConnectionMiddleware(context => Task.CompletedTask, httpConnectionAdapterOptions);

            Assert.Equal(HttpProtocols.Http1AndHttp2, httpConnectionAdapterOptions.HttpProtocols);
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Error logic only applies on Windows")]
        [MaximumOSVersion(OperatingSystems.Windows, WindowsVersions.Win81)]
        public void Http2ThrowsOnIncompatibleWindowsVersions()
        {
            var httpConnectionAdapterOptions = new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                HttpProtocols = HttpProtocols.Http2
            };

            Assert.Throws<NotSupportedException>(() => new HttpsConnectionMiddleware(context => Task.CompletedTask, httpConnectionAdapterOptions));
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.MacOSX | OperatingSystems.Linux, SkipReason = "Error logic only applies on Windows")]
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10)]
        public void Http2DoesNotThrowOnCompatibleWindowsVersions()
        {
            var httpConnectionAdapterOptions = new HttpsConnectionAdapterOptions
            {
                ServerCertificate = _x509Certificate2,
                HttpProtocols = HttpProtocols.Http2
            };

            // Does not throw
            new HttpsConnectionMiddleware(context => Task.CompletedTask, httpConnectionAdapterOptions);
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
}
