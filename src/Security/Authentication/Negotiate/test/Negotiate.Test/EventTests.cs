// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

public class EventTests
{
    [Fact]
    public async Task OnChallenge_Fires()
    {
        var eventInvoked = false;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnChallenge = context =>
                {
                    // Not changed yet
                    eventInvoked = true;
                    Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
                    Assert.False(context.Response.Headers.ContainsKey(HeaderNames.WWWAuthenticate));
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var result = await SendAsync(server, "/Authenticate", new TestConnection());
        Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
        Assert.Equal("Negotiate", result.Response.Headers.WWWAuthenticate);
        Assert.True(eventInvoked);
    }

    [Fact]
    public async Task OnChallenge_Handled()
    {
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnChallenge = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status418ImATeapot;
                    context.Response.Headers.WWWAuthenticate = "Teapot";
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var result = await SendAsync(server, "/Authenticate", new TestConnection());
        Assert.Equal(StatusCodes.Status418ImATeapot, result.Response.StatusCode);
        Assert.Equal("Teapot", result.Response.Headers.WWWAuthenticate);
    }

    [Fact]
    public async Task OnAuthenticationFailed_FromException_Fires()
    {
        var eventInvoked = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnAuthenticationFailed = context =>
                {
                    eventInvoked++;
                    Assert.IsType<InvalidOperationException>(context.Exception);
                    Assert.Equal("InvalidBlob", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SendAsync(server, "/404", new TestConnection(), "Negotiate InvalidBlob"));
        Assert.Equal("InvalidBlob", ex.Message);
        Assert.Equal(1, eventInvoked);
    }

    [Fact]
    public async Task OnAuthenticationFailed_FromException_Handled()
    {
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnAuthenticationFailed = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status418ImATeapot;
                    context.Response.Headers.WWWAuthenticate = "Teapot";
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var result = await SendAsync(server, "/404", new TestConnection(), "Negotiate InvalidBlob");
        Assert.Equal(StatusCodes.Status418ImATeapot, result.Response.StatusCode);
        Assert.Equal("Teapot", result.Response.Headers.WWWAuthenticate);
    }

    [Fact]
    public async Task OnAuthenticationFailed_FromOtherBlobError_Fires()
    {
        var eventInvoked = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnAuthenticationFailed = context =>
                {
                    eventInvoked++;
                    Assert.IsType<Exception>(context.Exception);
                    Assert.Equal("A test other error occurred", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var ex = await Assert.ThrowsAsync<Exception>(() =>
            SendAsync(server, "/404", new TestConnection(), "Negotiate OtherError"));
        Assert.Equal("A test other error occurred", ex.Message);
        Assert.Equal(1, eventInvoked);
    }

    [Fact]
    public async Task OnAuthenticationFailed_FromOtherBlobError_Handled()
    {
        var eventInvoked = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnAuthenticationFailed = context =>
                {
                    eventInvoked++;
                    context.Response.StatusCode = StatusCodes.Status418ImATeapot;
                    context.Response.Headers.WWWAuthenticate = "Teapot";
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var result = await SendAsync(server, "/404", new TestConnection(), "Negotiate OtherError");
        Assert.Equal(StatusCodes.Status418ImATeapot, result.Response.StatusCode);
        Assert.Equal("Teapot", result.Response.Headers.WWWAuthenticate);
        Assert.Equal(1, eventInvoked);
    }

    [Fact]
    public async Task OnAuthenticationFailed_FromCredentialError_Fires()
    {
        var eventInvoked = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnAuthenticationFailed = context =>
                {
                    eventInvoked++;
                    Assert.IsType<Exception>(context.Exception);
                    Assert.Equal("A test credential error occurred", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var response = await SendAsync(server, "/418", new TestConnection(), "Negotiate CredentialError");
        Assert.Equal(StatusCodes.Status418ImATeapot, response.Response.StatusCode);
        Assert.Equal(1, eventInvoked);
    }

    [Fact]
    public async Task OnAuthenticationFailed_FromCredentialError_Handled()
    {
        var eventInvoked = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnAuthenticationFailed = context =>
                {
                    eventInvoked++;
                    context.Response.StatusCode = StatusCodes.Status418ImATeapot;
                    context.Response.Headers.WWWAuthenticate = "Teapot";
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var result = await SendAsync(server, "/404", new TestConnection(), "Negotiate CredentialError");
        Assert.Equal(StatusCodes.Status418ImATeapot, result.Response.StatusCode);
        Assert.Equal("Teapot", result.Response.Headers.WWWAuthenticate);
        Assert.Equal(1, eventInvoked);
    }

    [Fact]
    public async Task OnAuthenticationFailed_FromClientError_Fires()
    {
        var eventInvoked = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnAuthenticationFailed = context =>
                {
                    eventInvoked++;
                    Assert.IsType<Exception>(context.Exception);
                    Assert.Equal("A test client error occurred", context.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var response = await SendAsync(server, "/404", new TestConnection(), "Negotiate ClientError");
        Assert.Equal(StatusCodes.Status400BadRequest, response.Response.StatusCode);
        Assert.Equal(1, eventInvoked);
    }

    [Fact]
    public async Task OnAuthenticationFailed_FromClientError_Handled()
    {
        var eventInvoked = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnAuthenticationFailed = context =>
                {
                    eventInvoked++;
                    context.Response.StatusCode = StatusCodes.Status418ImATeapot;
                    context.Response.Headers.WWWAuthenticate = "Teapot";
                    context.HandleResponse();
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var result = await SendAsync(server, "/404", new TestConnection(), "Negotiate ClientError");
        Assert.Equal(StatusCodes.Status418ImATeapot, result.Response.StatusCode);
        Assert.Equal("Teapot", result.Response.Headers.WWWAuthenticate);
        Assert.Equal(1, eventInvoked);
    }

    [Fact]
    public async Task OnAuthenticated_FiresOncePerRequest()
    {
        var callCount = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.PersistKerberosCredentials = true;
            options.Events = new NegotiateEvents()
            {
                OnAuthenticated = context =>
                {
                    var identity = context.Principal.Identity;
                    Assert.True(identity.IsAuthenticated);
                    Assert.Equal("name", identity.Name);
                    Assert.Equal("Kerberos", identity.AuthenticationType);
                    callCount++;
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var testConnection = new TestConnection();
        await KerberosStage1And2Auth(server, testConnection);
        var result = await SendAsync(server, "/Authenticate", testConnection);
        Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
        Assert.False(result.Response.Headers.ContainsKey(HeaderNames.WWWAuthenticate));
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task OnAuthenticated_Success_Continues()
    {
        var callCount = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnAuthenticated = context =>
                {
                    context.Success();
                    callCount++;
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        await KerberosStage1And2Auth(server, new TestConnection());
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task OnAuthenticated_NoResult_SuppresesCredentials()
    {
        var callCount = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnAuthenticated = context =>
                {
                    context.NoResult();
                    callCount++;
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var result = await SendAsync(server, "/Authenticate", new TestConnection(), "Negotiate ClientKerberosBlob");
        Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
        Assert.Equal("Negotiate", result.Response.Headers.WWWAuthenticate);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task OnAuthenticated_Fail_SuppresesCredentials()
    {
        var callCount = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnAuthenticated = context =>
                {
                    callCount++;
                    context.Fail("Event error.");
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        var result = await SendAsync(server, "/Authenticate", new TestConnection(), "Negotiate ClientKerberosBlob");
        Assert.Equal(StatusCodes.Status401Unauthorized, result.Response.StatusCode);
        Assert.Equal("Negotiate", result.Response.Headers.WWWAuthenticate);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task OnRetrieveLdapClaims_DoesNotFireWhenLdapDisabled()
    {
        var callCount = 0;
        using var host = await CreateHostAsync(options =>
        {
            options.Events = new NegotiateEvents()
            {
                OnRetrieveLdapClaims = context =>
                {
                    callCount++;
                    return Task.CompletedTask;
                }
            };
        });
        var server = host.GetTestServer();

        await KerberosStage1And2Auth(server, new TestConnection());
        Assert.Equal(0, callCount);
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
        Assert.Equal("Negotiate ServerKerberosBlob1", result.Response.Headers.WWWAuthenticate);
    }

    private static async Task KerberosStage2Auth(TestServer server, TestConnection testConnection)
    {
        var result = await SendAsync(server, "/Authenticate", testConnection, "Negotiate ClientKerberosBlob2");
        Assert.Equal(StatusCodes.Status200OK, result.Response.StatusCode);
        Assert.Equal("Negotiate ServerKerberosBlob2", result.Response.Headers.WWWAuthenticate);
    }

    private static async Task<IHost> CreateHostAsync(Action<NegotiateOptions> configureOptions = null)
    {
        var builder = new HostBuilder()
            .ConfigureServices(services => services
                .AddRouting()
                .AddAuthentication()
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

        builder.Map("/418", context =>
        {
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;
            return Task.CompletedTask;
        });
    }

    private static Task<HttpContext> SendAsync(TestServer server, string path, TestConnection connection, string authorizationHeader = null)
    {
        return server.SendAsync(context =>
        {
            context.Request.Path = path;
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                context.Request.Headers.Authorization = authorizationHeader;
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
