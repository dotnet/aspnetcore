// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.NamedPipes.Tests;

public class WebHostTests : LoggedTest
{
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Test expects not supported error. Skip Windows because named pipes supports Windows.")]
    public async Task ListenNamedPipeEndpoint_NonWindowsOperatingSystem_ErrorAsync()
    {
        // Arrange
        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.ListenNamedPipe("Pipename");
                    })
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("hello, world");
                        });
                    });
            });

        using var host = builder.Build();

        // Act
        var ex = await Assert.ThrowsAsync<PlatformNotSupportedException>(() => host.StartAsync());

        // Assert
        Assert.Equal("Named pipes transport requires a Windows operating system.", ex.Message);
    }

    [Fact]
    public async Task ListenNamedPipeEndpoint_CustomNamedPipeEndpointTransport()
    {
        // Arrange
        var transport = new TestConnectionListenerFactory();

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.ListenNamedPipe("Pipename");
                    })
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("hello, world");
                        });
                    });
                webHostBuilder.ConfigureServices(services =>
                 {
                     services.AddSingleton<IConnectionListenerFactory>(transport);
                 });
            });

        using var host = builder.Build();

        // Act
        await host.StartAsync();
        await host.StopAsync();

        // Assert
        Assert.Equal("Pipename", transport.BoundEndPoint.PipeName);
    }

    private sealed class TestConnectionListenerFactory : IConnectionListenerFactory, IConnectionListenerFactorySelector
    {
        public NamedPipeEndPoint BoundEndPoint { get; private set; }

        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<IConnectionListener>(new TestConnectionListener());
        }

        public bool CanBind(EndPoint endpoint)
        {
            if (endpoint is NamedPipeEndPoint ep)
            {
                BoundEndPoint = ep;
                return true;
            }
            return false;
        }

        private sealed class TestConnectionListener : IConnectionListener
        {
            public EndPoint EndPoint { get; }

            public ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult<ConnectionContext>(null);
            }

            public ValueTask DisposeAsync()
            {
                return default;
            }

            public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
            {
                return default;
            }
        }
    }

    [ConditionalFact]
    [NamedPipesSupported]
    public async Task ListenNamedPipeEndpoint_HelloWorld_ClientSuccess()
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);
        var pipeName = NamedPipeTestHelpers.GetUniquePipeName();

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.ListenNamedPipe(pipeName);
                    })
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("hello, world");
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

        using (var host = builder.Build())
        using (var client = CreateClient(pipeName))
        {
            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1/")
            {
                Version = HttpVersion.Version11,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };

            // Act
            var response = await client.SendAsync(request).DefaultTimeout();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version11, response.Version);
            var responseText = await response.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("hello, world", responseText);

            await host.StopAsync().DefaultTimeout();
        }
    }

    [ConditionalFact]
    [NamedPipesSupported]
    public async Task ListenNamedPipeEndpoint_Impersonation_ClientSuccess()
    {
        AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);
        var pipeName = NamedPipeTestHelpers.GetUniquePipeName();

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.ListenNamedPipe(pipeName, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });
                    })
                    .UseNamedPipes(options =>
                    {
                        var ps = new PipeSecurity();
                        ps.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));

                        options.PipeSecurity = ps;
                        options.CurrentUserOnly = false;
                    })
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            var serverName = Thread.CurrentPrincipal.Identity.Name;

                            var namedPipeStream = context.Features.Get<IConnectionNamedPipeFeature>().NamedPipe;
                            var impersonatedName = namedPipeStream.GetImpersonationUserName();

                            context.Response.Headers.Add("X-Server-Identity", serverName);
                            context.Response.Headers.Add("X-Impersonated-Identity", impersonatedName);

                            var buffer = new byte[1024];
                            while (await context.Request.Body.ReadAsync(buffer) != 0)
                            {

                            }

                            await context.Response.WriteAsync("hello, world");
                        });
                    });
            })
            .ConfigureServices(services =>
            {
                AddTestLogging(services);
            });

        using (var host = builder.Build())
        using (var client = CreateClient(pipeName, TokenImpersonationLevel.Impersonation))
        {
            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1/")
            {
                Version = HttpVersion.Version11,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact,
                Content = new ByteArrayContent(Encoding.UTF8.GetBytes(new string('c', 1024 * 1024)))
            };

            // Act
            var response = await client.SendAsync(request).DefaultTimeout();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version11, response.Version);
            var responseText = await response.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("hello, world", responseText);

            var serverIdentity = string.Join(",", response.Headers.GetValues("X-Server-Identity"));
            var impersonatedIdentity = string.Join(",", response.Headers.GetValues("X-Impersonated-Identity"));

            Assert.Equal(serverIdentity.Split('\\')[1], impersonatedIdentity);

            await host.StopAsync().DefaultTimeout();
        }
    }

    [ConditionalFact]
    [NamedPipesSupported]
    public async Task ListenNamedPipeEndpoint_Security_PerEndpointSecuritySettings()
    {
        AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);

        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);
        var defaultSecurityPipeName = NamedPipeTestHelpers.GetUniquePipeName();
        var customSecurityPipeName = NamedPipeTestHelpers.GetUniquePipeName();

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.ListenNamedPipe(defaultSecurityPipeName, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });
                        o.ListenNamedPipe(customSecurityPipeName, listenOptions =>
                        {
                            listenOptions.Protocols = HttpProtocols.Http1;
                        });
                    })
                    .UseNamedPipes(options =>
                    {
                        var defaultSecurity = new PipeSecurity();
                        defaultSecurity.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));

                        options.PipeSecurity = defaultSecurity;
                        options.CurrentUserOnly = false;
                        options.CreateNamedPipeServerStream = (context) =>
                        {
                            if (context.NamedPipeEndPoint.PipeName == defaultSecurityPipeName)
                            {
                                return NamedPipeTransportOptions.CreateDefaultNamedPipeServerStream(context);
                            }

                            var allowSecurity = new PipeSecurity();
                            allowSecurity.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.FullControl, AccessControlType.Allow));

                            return NamedPipeServerStreamAcl.Create(
                                context.NamedPipeEndPoint.PipeName,
                                PipeDirection.InOut,
                                NamedPipeServerStream.MaxAllowedServerInstances,
                                PipeTransmissionMode.Byte,
                                context.PipeOptions,
                                inBufferSize: 0, // Buffer in System.IO.Pipelines
                                outBufferSize: 0, // Buffer in System.IO.Pipelines
                                allowSecurity);
                        };
                    })
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            var serverName = Thread.CurrentPrincipal.Identity.Name;

                            var namedPipeStream = context.Features.Get<IConnectionNamedPipeFeature>().NamedPipe;

                            var security = namedPipeStream.GetAccessControl();
                            var rules = security.GetAccessRules(includeExplicit: true, includeInherited: false, typeof(SecurityIdentifier));

                            context.Response.Headers.Add("X-PipeAccessRights", ((int)rules.OfType<PipeAccessRule>().Single().PipeAccessRights).ToString(CultureInfo.InvariantCulture));

                            await context.Response.WriteAsync("hello, world");
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

        using (var host = builder.Build())
        {
            await host.StartAsync().DefaultTimeout();

            using (var client = CreateClient(defaultSecurityPipeName))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1/")
                {
                    Version = HttpVersion.Version11,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact
                };

                // Act
                var response = await client.SendAsync(request).DefaultTimeout();

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(HttpVersion.Version11, response.Version);
                var responseText = await response.Content.ReadAsStringAsync().DefaultTimeout();
                Assert.Equal("hello, world", responseText);

                var pipeAccessRights = (PipeAccessRights)Convert.ToInt32(string.Join(",", response.Headers.GetValues("X-PipeAccessRights")), CultureInfo.InvariantCulture); 

                Assert.Equal(PipeAccessRights.ReadWrite, pipeAccessRights & PipeAccessRights.ReadWrite);
                Assert.Equal(PipeAccessRights.CreateNewInstance, pipeAccessRights & PipeAccessRights.CreateNewInstance);
            }

            using (var client = CreateClient(customSecurityPipeName))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1/")
                {
                    Version = HttpVersion.Version11,
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact
                };

                // Act
                var response = await client.SendAsync(request).DefaultTimeout();

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal(HttpVersion.Version11, response.Version);
                var responseText = await response.Content.ReadAsStringAsync().DefaultTimeout();
                Assert.Equal("hello, world", responseText);

                var pipeAccessRights = (PipeAccessRights)Convert.ToInt32(string.Join(",", response.Headers.GetValues("X-PipeAccessRights")), CultureInfo.InvariantCulture);

                Assert.Equal(PipeAccessRights.FullControl, pipeAccessRights & PipeAccessRights.FullControl);
            }

            await host.StopAsync().DefaultTimeout();
        }
    }

    [ConditionalTheory]
    [NamedPipesSupported]
    [InlineData(HttpProtocols.Http1)]
    [InlineData(HttpProtocols.Http2)]
    public async Task ListenNamedPipeEndpoint_ProtocolVersion_ClientSuccess(HttpProtocols protocols)
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);
        var pipeName = NamedPipeTestHelpers.GetUniquePipeName();
        var clientVersion = GetClientVersion(protocols);

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.ListenNamedPipe(pipeName, options =>
                        {
                            options.Protocols = protocols;
                        });
                    })
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("hello, world");
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

        using (var host = builder.Build())
        using (var client = CreateClient(pipeName))
        {
            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1/")
            {
                Version = clientVersion,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };

            // Act
            var response = await client.SendAsync(request).DefaultTimeout();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(clientVersion, response.Version);
            var responseText = await response.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("hello, world", responseText);

            await host.StopAsync().DefaultTimeout();
        }
    }

    private static Version GetClientVersion(HttpProtocols protocols)
    {
        return protocols switch
        {
            HttpProtocols.Http1 => HttpVersion.Version11,
            HttpProtocols.Http2 => HttpVersion.Version20,
            _ => throw new InvalidOperationException(),
        };
    }

    [ConditionalTheory]
    [TlsAlpnSupported]
    [NamedPipesSupported]
    [InlineData(HttpProtocols.Http1)]
    [InlineData(HttpProtocols.Http2)]
    public async Task ListenNamedPipeEndpoint_Tls_ClientSuccess(HttpProtocols protocols)
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);
        var pipeName = NamedPipeTestHelpers.GetUniquePipeName();
        var clientVersion = GetClientVersion(protocols);

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseKestrel(o =>
                    {
                        o.ListenNamedPipe(pipeName, options =>
                        {
                            options.Protocols = protocols;
                            options.UseHttps(TestResources.GetTestCertificate());
                        });
                    })
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("hello, world");
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

        using (var host = builder.Build())
        using (var client = CreateClient(pipeName))
        {
            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://127.0.0.1/")
            {
                Version = clientVersion,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };

            // Act
            var response = await client.SendAsync(request).DefaultTimeout();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(clientVersion, response.Version);
            var responseText = await response.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("hello, world", responseText);

            await host.StopAsync().DefaultTimeout();
        }
    }

    [ConditionalFact]
    [NamedPipesSupported]
    public async Task ListenNamedPipeEndpoint_FromUrl_HelloWorld_ClientSuccess()
    {
        // Arrange
        using var httpEventSource = new HttpEventSourceListener(LoggerFactory);
        var pipeName = NamedPipeTestHelpers.GetUniquePipeName();
        var url = $"http://pipe:/{pipeName}";

        var builder = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .UseUrls(url)
                    .UseKestrel()
                    .Configure(app =>
                    {
                        app.Run(async context =>
                        {
                            await context.Response.WriteAsync("hello, world");
                        });
                    });
            })
            .ConfigureServices(AddTestLogging);

        using (var host = builder.Build())
        using (var client = CreateClient(pipeName))
        {
            await host.StartAsync().DefaultTimeout();

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1/")
            {
                Version = HttpVersion.Version11,
                VersionPolicy = HttpVersionPolicy.RequestVersionExact
            };

            // Act
            var response = await client.SendAsync(request).DefaultTimeout();

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version11, response.Version);
            var responseText = await response.Content.ReadAsStringAsync().DefaultTimeout();
            Assert.Equal("hello, world", responseText);

            await host.StopAsync().DefaultTimeout();
        }

        var listeningOn = TestSink.Writes.Single(m => m.EventId.Name == "ListeningOnAddress");
        Assert.Equal($"Now listening on: {url}", listeningOn.Message);
    }

    private static HttpClient CreateClient(string pipeName, TokenImpersonationLevel? impersonationLevel = null)
    {
        var httpHandler = new SocketsHttpHandler
        {
            SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, __, ___, ____) => true
            }
        };

        var connectionFactory = new NamedPipesConnectionFactory(pipeName, impersonationLevel);
        httpHandler.ConnectCallback = connectionFactory.ConnectAsync;

        return new HttpClient(httpHandler);
    }

    public class NamedPipesConnectionFactory
    {
        private readonly string _pipeName;
        private readonly TokenImpersonationLevel? _impersonationLevel;

        public NamedPipesConnectionFactory(string pipeName, TokenImpersonationLevel? impersonationLevel = null)
        {
            _pipeName = pipeName;
            _impersonationLevel = impersonationLevel;
        }

        public async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext _,
            CancellationToken cancellationToken = default)
        {
            var clientStream = new NamedPipeClientStream(
                serverName: ".",
                pipeName: _pipeName,
                direction: PipeDirection.InOut,
                options: PipeOptions.WriteThrough | PipeOptions.Asynchronous,
                impersonationLevel: _impersonationLevel ?? TokenImpersonationLevel.Anonymous);

            try
            {
                await clientStream.ConnectAsync(cancellationToken).ConfigureAwait(false);
                return clientStream;
            }
            catch
            {
                clientStream.Dispose();
                throw;
            }
        }
    }
}
