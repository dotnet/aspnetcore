// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    [QuarantinedTest]
    public class NegotiateHandlerTests
    {
        [Fact]
        public async Task Anonymous_MissingConnectionFeatures_ThrowsNotSupported()
        {
            using var host = await CreateHostAsync();
            var server = host.GetTestServer();

            var ex = await Assert.ThrowsAsync<NotSupportedException>(() => SendAsync(server, "/Anonymous1", connection: null));
            Assert.Equal("Negotiate authentication requires a server that supports IConnectionItemsFeature like Kestrel.", ex.Message);
        }

        [Fact]
        public async Task Anonymous_NoChallenge_NoOps()
        {
            using var host = await CreateHostAsync();
            var server = host.GetTestServer();

            var result = await SendAsync(server, "/Anonymous1", new TestConnection());
            Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
        }

        [Fact]
        public async Task Anonymous_Http2_NoOps()
        {
            using var host = await CreateHostAsync();
            var server = host.GetTestServer();

            var result = await SendAsync(server, "/Anonymous2", connection: null, http2: true);
            Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
        }

        [Fact]
        public async Task Anonymous_Challenge_401Negotiate()
        {
            using var host = await CreateHostAsync();
            var server = host.GetTestServer();

            var result = await SendAsync(server, "/Authenticate", new TestConnection());
            Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
            Assert.Equal("Negotiate", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        [Fact]
        public async Task Anonymous_ChallengeHttp2_401Negotiate()
        {
            using var host = await CreateHostAsync();
            var server = host.GetTestServer();

            var result = await SendAsync(server, "/Authenticate", connection: null, http2: true);
            // Clients will downgrade to HTTP/1.1 and authenticate.
            Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
            Assert.Equal("Negotiate", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        [Fact]
        public async Task NtlmStage1Auth_401NegotiateServerBlob1()
        {
            using var host = await CreateHostAsync();
            var server = host.GetTestServer();
            var result = await SendAsync(server, "/404", new TestConnection(), "Negotiate ClientNtlmBlob1");
            Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
            Assert.Equal("Negotiate ServerNtlmBlob1", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        [Fact]
        public async Task AnonymousAfterNtlmStage1_Throws()
        {
            using var host = await CreateHostAsync();
            var server = host.GetTestServer();
            var testConnection = new TestConnection();
            await NtlmStage1Auth(server, testConnection);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => SendAsync(server, "/404", testConnection));
            Assert.Equal("An anonymous request was received in between authentication handshake requests.", ex.Message);
        }

        [Fact]
        public async Task NtlmStage2Auth_WithoutStage1_Throws()
        {
            using var host = await CreateHostAsync();
            var server = host.GetTestServer();

            var ex = await Assert.ThrowsAsync<TrueException>(() => SendAsync(server, "/404", new TestConnection(), "Negotiate ClientNtlmBlob2"));
            Assert.Equal("Stage1Complete", ex.UserMessage);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task NtlmStage1And2Auth_Success(bool persistNtlm)
        {
            using var host = await CreateHostAsync(options => options.PersistNtlmCredentials = persistNtlm);
            var server = host.GetTestServer();
            var testConnection = new TestConnection();
            await NtlmStage1And2Auth(server, testConnection);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task KerberosAuth_Success(bool persistKerberos)
        {
            using var host = await CreateHostAsync(options => options.PersistKerberosCredentials = persistKerberos);
            var server = host.GetTestServer();
            var testConnection = new TestConnection();
            await KerberosAuth(server, testConnection);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task KerberosTwoStageAuth_Success(bool persistKerberos)
        {
            using var host = await CreateHostAsync(options => options.PersistKerberosCredentials = persistKerberos);
            var server = host.GetTestServer();
            var testConnection = new TestConnection();
            await KerberosStage1And2Auth(server, testConnection);
        }

        [Theory]
        [InlineData("NTLM")]
        [InlineData("Kerberos")]
        [InlineData("Kerberos2")]
        public async Task AnonymousAfterCompletedPersist_Cached(string protocol)
        {
            using var host = await CreateHostAsync(options => options.PersistNtlmCredentials = options.PersistKerberosCredentials = true);
            var server = host.GetTestServer();
            var testConnection = new TestConnection();
            if (protocol == "NTLM")
            {
                await NtlmStage1And2Auth(server, testConnection);
            }
            else if (protocol == "Kerberos2")
            {
                await KerberosStage1And2Auth(server, testConnection);
            }
            else
            {
                await KerberosAuth(server, testConnection);
            }

            var result = await SendAsync(server, "/Authenticate", testConnection);
            Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
            Assert.False(result.Response.Headers.ContainsKey(HeaderNames.WWWAuthenticate));
        }

        [Theory]
        [InlineData("NTLM")]
        [InlineData("Kerberos")]
        [InlineData("Kerberos2")]
        public async Task AnonymousAfterCompletedNoPersist_Denied(string protocol)
        {
            using var host = await CreateHostAsync(options => options.PersistNtlmCredentials = options.PersistKerberosCredentials = false);
            var server = host.GetTestServer();
            var testConnection = new TestConnection();
            if (protocol == "NTLM")
            {
                await NtlmStage1And2Auth(server, testConnection);
            }
            else if (protocol == "Kerberos2")
            {
                await KerberosStage1And2Auth(server, testConnection);
            }
            else
            {
                await KerberosAuth(server, testConnection);
            }

            var result = await SendAsync(server, "/Authenticate", testConnection);
            Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
            Assert.Equal("Negotiate", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AuthHeaderAfterNtlmCompleted_ReAuthenticates(bool persist)
        {
            using var host = await CreateHostAsync(options => options.PersistNtlmCredentials = persist);
            var server = host.GetTestServer();
            var testConnection = new TestConnection();
            await NtlmStage1And2Auth(server, testConnection);
            await NtlmStage1And2Auth(server, testConnection);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AuthHeaderAfterKerberosCompleted_ReAuthenticates(bool persist)
        {
            using var host = await CreateHostAsync(options => options.PersistNtlmCredentials = persist);
            var server = host.GetTestServer();
            var testConnection = new TestConnection();
            await KerberosAuth(server, testConnection);
            await KerberosAuth(server, testConnection);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AuthHeaderAfterKerberos2StageCompleted_ReAuthenticates(bool persist)
        {
            using var host = await CreateHostAsync(options => options.PersistNtlmCredentials = persist);
            var server = host.GetTestServer();
            var testConnection = new TestConnection();
            await KerberosStage1And2Auth(server, testConnection);
            await KerberosStage1And2Auth(server, testConnection);
        }

        [Fact]
        public async Task ApplicationExceptionReExecute_AfterComplete_DoesntReRun()
        {
            var builder = new HostBuilder()
                .ConfigureServices(services => services
                    .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                    .AddNegotiate(options =>
                    {
                        options.StateFactory = new TestNegotiateStateFactory();
                    }))
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseTestServer();
                    webHostBuilder.Configure(app =>
                    {
                        app.UseExceptionHandler("/error");
                        app.UseAuthentication();
                        app.Run(context =>
                        {
                            Assert.True(context.User.Identity.IsAuthenticated);
                            if (context.Request.Path.Equals("/error"))
                            {
                                return context.Response.WriteAsync("Error Handler");
                            }

                            throw new TimeZoneNotFoundException();
                        });
                    });
                });

            using var host = await builder.StartAsync();
            var server = host.GetTestServer();

            var testConnection = new TestConnection();
            await NtlmStage1Auth(server, testConnection);
            var result = await SendAsync(server, "/Authenticate", testConnection, "Negotiate ClientNtlmBlob2");
            Assert.Equal(StatusCodes.Status500InternalServerError, result.Response.StatusCode);
            Assert.False(result.Response.Headers.ContainsKey(HeaderNames.WWWAuthenticate));
        }

        [Fact]
        public async Task CredentialError_401()
        {
            using var host = await CreateHostAsync();
            var server = host.GetTestServer();
            var testConnection = new TestConnection();
            var result = await SendAsync(server, "/Authenticate", testConnection, "Negotiate CredentialError");
            Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
            Assert.Equal("Negotiate", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        [Fact]
        public async Task ClientError_400()
        {
            using var host = await CreateHostAsync();
            var server = host.GetTestServer();
            var testConnection = new TestConnection();
            var result = await SendAsync(server, "/404", testConnection, "Negotiate ClientError");
            Assert.Equal(StatusCodes.Status400BadRequest, result.Response.StatusCode);
            Assert.DoesNotContain(HeaderNames.WWWAuthenticate, result.Response.Headers);
        }

        [Fact]
        public async Task OtherError_Throws()
        {
            using var host = await CreateHostAsync();
            var server = host.GetTestServer();
            var testConnection = new TestConnection();

            var ex = await Assert.ThrowsAsync<Exception>(() => SendAsync(server, "/404", testConnection, "Negotiate OtherError"));
            Assert.Equal("A test other error occurred", ex.Message);
        }

        // Single Stage
        private static async Task KerberosAuth(TestServer server, TestConnection testConnection)
        {
            var result = await SendAsync(server, "/Authenticate", testConnection, "Negotiate ClientKerberosBlob");
            Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
            Assert.Equal("Negotiate ServerKerberosBlob", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        private static async Task KerberosStage1And2Auth(TestServer server, TestConnection testConnection)
        {
            await KerberosStage1Auth(server, testConnection);
            await KerberosStage2Auth(server, testConnection);
        }

        private static async Task KerberosStage1Auth(TestServer server, TestConnection testConnection)
        {
            var result = await SendAsync(server, "/Authenticate", testConnection, "Negotiate ClientKerberosBlob1");
            Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
            Assert.Equal("Negotiate ServerKerberosBlob1", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        private static async Task KerberosStage2Auth(TestServer server, TestConnection testConnection)
        {
            var result = await SendAsync(server, "/Authenticate", testConnection, "Negotiate ClientKerberosBlob2");
            Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
            Assert.Equal("Negotiate ServerKerberosBlob2", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        private static async Task NtlmStage1And2Auth(TestServer server, TestConnection testConnection)
        {
            await NtlmStage1Auth(server, testConnection);
            await NtlmStage2Auth(server, testConnection);
        }

        private static async Task NtlmStage1Auth(TestServer server, TestConnection testConnection)
        {
            var result = await SendAsync(server, "/404", testConnection, "Negotiate ClientNtlmBlob1");
            Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
            Assert.Equal("Negotiate ServerNtlmBlob1", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        private static async Task NtlmStage2Auth(TestServer server, TestConnection testConnection)
        {
            var result = await SendAsync(server, "/Authenticate", testConnection, "Negotiate ClientNtlmBlob2");
            Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
            Assert.Equal("Negotiate ServerNtlmBlob2", result.Response.Headers[HeaderNames.WWWAuthenticate]);
        }

        private static async Task<IHost> CreateHostAsync(Action<NegotiateOptions> configureOptions = null)
        {
            var builder = new HostBuilder()
                .ConfigureServices(services => services
                    .AddRouting()
                    .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                    .AddNegotiate(options =>
                    {
                        options.StateFactory = new TestNegotiateStateFactory();
                        configureOptions?.Invoke(options);
                    }))
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder.UseTestServer();
                    webHostBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseEndpoints(ConfigureEndpoints);
                    });
                });

            return await builder.StartAsync();
        }

        private static void ConfigureEndpoints(IEndpointRouteBuilder builder)
        {
            builder.Map("/Anonymous1", context =>
            {
                Assert.Equal("HTTP/1.1", context.Request.Protocol);
                Assert.False(context.User.Identity.IsAuthenticated, "Anonymous");
                return Task.CompletedTask;
            });

            builder.Map("/Anonymous2", context =>
            {
                Assert.Equal("HTTP/2", context.Request.Protocol);
                Assert.False(context.User.Identity.IsAuthenticated, "Anonymous");
                return Task.CompletedTask;
            });

            builder.Map("/Authenticate", async context =>
            {
                if (!context.User.Identity.IsAuthenticated)
                {
                    await context.ChallengeAsync();
                    return;
                }

                Assert.Equal("HTTP/1.1", context.Request.Protocol); // Not HTTP/2
                var name = context.User.Identity.Name;
                Assert.False(string.IsNullOrEmpty(name), "name");
                await context.Response.WriteAsync(name);
            });

            builder.Map("/AlreadyAuthenticated", async context =>
            {
                Assert.Equal("HTTP/1.1", context.Request.Protocol); // Not HTTP/2
                Assert.True(context.User.Identity.IsAuthenticated, "Authenticated");
                var name = context.User.Identity.Name;
                Assert.False(string.IsNullOrEmpty(name), "name");
                await context.Response.WriteAsync(name);
            });

            builder.Map("/Unauthorized", async context =>
            {
                // Simulate Authorization failure
                var result = await context.AuthenticateAsync();
                await context.ChallengeAsync();
            });

            builder.Map("/SignIn", context =>
            {
                return Assert.ThrowsAsync<InvalidOperationException>(() => context.SignInAsync(new ClaimsPrincipal()));
            });

            builder.Map("/signOut", context =>
            {
                return Assert.ThrowsAsync<InvalidOperationException>(() => context.SignOutAsync());
            });
        }

        private static Task<HttpContext> SendAsync(TestServer server, string path, TestConnection connection, string authorizationHeader = null, bool http2 = false)
        {
            return server.SendAsync(context =>
            {
                context.Request.Protocol = http2 ? "HTTP/2" : "HTTP/1.1";
                context.Request.Path = path;
                if (!string.IsNullOrEmpty(authorizationHeader))
                {
                    context.Request.Headers[HeaderNames.Authorization] = authorizationHeader;
                }
                if (connection != null)
                {
                    context.Features.Set<IConnectionItemsFeature>(connection);
                    context.Features.Set<IConnectionCompleteFeature>(connection);
                }
            });
        }

        private class TestConnection : IConnectionItemsFeature, IConnectionCompleteFeature
        {
            public IDictionary<object, object> Items { get; set; } = new ConnectionItems();

            public void OnCompleted(Func<object, Task> callback, object state)
            {
            }
        }

        private class TestNegotiateStateFactory : INegotiateStateFactory
        {
            public INegotiateState CreateInstance() => new TestNegotiateState();
        }

        private class TestNegotiateState : INegotiateState
        {
            private string _protocol;
            private bool Stage1Complete { get; set; }
            public bool IsCompleted { get; private set; }
            public bool IsDisposed { get; private set; }

            public string Protocol
            {
                get
                {
                    if (IsDisposed)
                    {
                        throw new ObjectDisposedException(nameof(TestNegotiateState));
                    }
                    if (!Stage1Complete)
                    {
                        throw new InvalidOperationException("Authentication has not started yet.");
                    }
                    return _protocol;
                }
            }

            public void Dispose()
            {
                IsDisposed = true;
            }

            public IIdentity GetIdentity()
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException(nameof(TestNegotiateState));
                }
                if (!IsCompleted)
                {
                    throw new InvalidOperationException("Authentication is not complete.");
                }
                return new GenericIdentity("name", _protocol);
            }

            public string GetOutgoingBlob(string incomingBlob, out BlobErrorType errorType, out Exception ex)
            {
                if (IsDisposed)
                {
                    throw new ObjectDisposedException(nameof(TestNegotiateState));
                }
                if (IsCompleted)
                {
                    throw new InvalidOperationException("Authentication is already complete.");
                }

                errorType = BlobErrorType.None;
                ex = null;

                switch (incomingBlob)
                {
                    case "ClientNtlmBlob1":
                        Assert.False(Stage1Complete, nameof(Stage1Complete));
                        Stage1Complete = true;
                        _protocol = "NTLM";
                        return "ServerNtlmBlob1";
                    case "ClientNtlmBlob2":
                        Assert.True(Stage1Complete, nameof(Stage1Complete));
                        Assert.Equal("NTLM", _protocol);
                        IsCompleted = true;
                        return "ServerNtlmBlob2";
                    // Kerberos can require one or two stages
                    case "ClientKerberosBlob":
                        Assert.False(Stage1Complete, nameof(Stage1Complete));
                        _protocol = "Kerberos";
                        Stage1Complete = true;
                        IsCompleted = true;
                        return "ServerKerberosBlob";
                    case "ClientKerberosBlob1":
                        Assert.False(Stage1Complete, nameof(Stage1Complete));
                        _protocol = "Kerberos";
                        Stage1Complete = true;
                        return "ServerKerberosBlob1";
                    case "ClientKerberosBlob2":
                        Assert.True(Stage1Complete, nameof(Stage1Complete));
                        Assert.Equal("Kerberos", _protocol);
                        IsCompleted = true;
                        return "ServerKerberosBlob2";
                    case "CredentialError":
                        errorType = BlobErrorType.CredentialError;
                        ex = new Exception("A test credential error occurred");
                        return null;
                    case "ClientError":
                        errorType = BlobErrorType.ClientError;
                        ex = new Exception("A test client error occurred");
                        return null;
                    case "OtherError":
                        errorType = BlobErrorType.Other;
                        ex = new Exception("A test other error occurred");
                        return null;
                    default:
                        errorType = BlobErrorType.Other;
                        ex = new InvalidOperationException(incomingBlob);
                        return null;
                }
            }
        }
    }
}
