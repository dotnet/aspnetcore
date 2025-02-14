// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.Negotiate;

// In theory this would work on Linux and Mac, but the client would require explicit credentials.
[OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX)]
public class NegotiateHandlerFunctionalTests : LoggedTest
{
    private static readonly Version Http11Version = new Version(1, 1);
    private static readonly Version Http2Version = new Version(2, 0);

    public static IEnumerable<object[]> Http11And2 =>
        new List<object[]>
        {
                new object[] { Http11Version },
                new object[] { Http2Version },
        };

    [ConditionalFact]
    // Only test HTTP/1.1, ALPN is not supported on Win7
    public async Task Anonymous_NoChallenge_NoOps_Win7()
    {
        using var host = await CreateHostAsync();
        using var client = CreateSocketHttpClient(host);
        client.DefaultRequestVersion = Http11Version;

        var result = await client.GetAsync("/Anonymous1");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(result.Headers.Contains(HeaderNames.WWWAuthenticate));
        Assert.Equal(Http11Version, result.Version);
    }

    [ConditionalFact]
    // Only test HTTP/1.1, ALPN is not supported on Win7
    public async Task Anonymous_Challenge_401Negotiate_Win7()
    {
        using var host = await CreateHostAsync();
        // WinHttpHandler can't disable default credentials on localhost, use SocketHttpHandler.
        using var client = CreateSocketHttpClient(host);
        client.DefaultRequestVersion = Http11Version;

        var result = await client.GetAsync("/Authenticate");
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("Negotiate", result.Headers.WwwAuthenticate.ToString());
        Assert.Equal(Http11Version, result.Version);
    }

    [ConditionalTheory]
    [MemberData(nameof(Http11And2))]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Windows only supports ALPN and required ciphers on 10 and later.")]
    public async Task Anonymous_NoChallenge_NoOps(Version version)
    {
        using var host = await CreateHostAsync();
        using var client = CreateSocketHttpClient(host);
        client.DefaultRequestVersion = version;

        var result = await client.GetAsync("/Anonymous" + version.Major);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.False(result.Headers.Contains(HeaderNames.WWWAuthenticate));
        Assert.Equal(version, result.Version);
    }

    [ConditionalTheory]
    [MemberData(nameof(Http11And2))]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "Windows only supports ALPN and required ciphers on 10 and later.")]
    public async Task Anonymous_Challenge_401Negotiate(Version version)
    {
        using var host = await CreateHostAsync();
        // WinHttpHandler can't disable default credentials on localhost, use SocketHttpHandler.
        using var client = CreateSocketHttpClient(host);
        client.DefaultRequestVersion = version;

        var result = await client.GetAsync("/Authenticate");
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("Negotiate", result.Headers.WwwAuthenticate.ToString());
        Assert.Equal(version, result.Version);
    }

    [ConditionalTheory]
    [MemberData(nameof(Http11And2))]
    public async Task DefautCredentials_Success(Version version)
    {
        using var host = await CreateHostAsync();
        // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade HTTP/2. WinHttpHandler does.
        using var client = CreateWinHttpClient(host);
        client.DefaultRequestVersion = version;

        var result = await client.GetAsync("/Authenticate");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(Http11Version, result.Version); // HTTP/2 downgrades.
    }

    [ConditionalFact]
    public async Task DefautCredentials_WebSocket_Success()
    {
        using var host = await CreateHostAsync();

        var address = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First().Replace("https://", "wss://");

        using var webSocket = new ClientWebSocket
        {
            Options =
                {
                    RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true,
                    UseDefaultCredentials = true,
                }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        await webSocket.ConnectAsync(new Uri($"{address}/AuthenticateWebSocket"), cts.Token);

        var receiveBuffer = new byte[13];
        var receiveResult = await webSocket.ReceiveAsync(receiveBuffer, cts.Token);

        Assert.True(receiveResult.EndOfMessage);
        Assert.Equal(WebSocketMessageType.Text, receiveResult.MessageType);
        Assert.Equal("Hello World!", Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count));
    }

    public static IEnumerable<object[]> HttpOrders =>
        new List<object[]>
        {
                new object[] { Http11Version, Http11Version },
                new object[] { Http11Version, Http2Version },
                new object[] { Http2Version, Http11Version },
        };

    [ConditionalTheory]
    [MemberData(nameof(HttpOrders))]
    public async Task RequestAfterAuth_ReUses1WithPersistence(Version first, Version second)
    {
        using var host = await CreateHostAsync(options => options.PersistNtlmCredentials = true);
        // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade HTTP/2. WinHttpHandler does.
        using var client = CreateWinHttpClient(host);
        client.DefaultRequestVersion = first;

        var result = await client.GetAsync("/Authenticate");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(Http11Version, result.Version); // Http/2 downgrades

        // Re-uses the 1.1 connection.
        result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/AlreadyAuthenticated") { Version = second });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(Http11Version, result.Version);
    }

    [ConditionalTheory]
    [MemberData(nameof(HttpOrders))]
    public async Task RequestAfterAuth_ReauthenticatesWhenNotPersisted(Version first, Version second)
    {
        using var host = await CreateHostAsync(options => options.PersistNtlmCredentials = false);
        // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade HTTP/2. WinHttpHandler does.
        using var client = CreateWinHttpClient(host);
        client.DefaultRequestVersion = first;

        var result = await client.GetAsync("/Authenticate");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(Http11Version, result.Version); // Http/2 downgrades

        // Re-uses the 1.1 connection.
        result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/Authenticate") { Version = second });
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(Http11Version, result.Version);
    }

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestAfterAuth_Http2Then2_Success(bool persistNtlm)
    {
        using var host = await CreateHostAsync(options => options.PersistNtlmCredentials = persistNtlm);
        // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade HTTP/2. WinHttpHandler does.
        using var client = CreateWinHttpClient(host);
        client.DefaultRequestVersion = Http2Version;

        // Falls back to HTTP/1.1 after trying HTTP/2.
        var result = await client.GetAsync("/Authenticate");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(Http11Version, result.Version);

        // Tries HTTP/2, falls back to HTTP/1.1 and re-authenticates.
        result = await client.GetAsync("/Authenticate");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(Http11Version, result.Version);
    }

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win10, SkipReason = "The client only supports HTTP/2 on Win10.")]
    public async Task RequestAfterAuth_Http2Then2Anonymous_Success(bool persistNtlm)
    {
        using var host = await CreateHostAsync(options => options.PersistNtlmCredentials = persistNtlm);
        // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade HTTP/2. WinHttpHandler does.
        using var client = CreateWinHttpClient(host);
        client.DefaultRequestVersion = Http2Version;

        // Falls back to HTTP/1.1 after trying HTTP/2.
        var result = await client.GetAsync("/Authenticate");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(Http11Version, result.Version);

        // Makes an anonymous HTTP/2 request
        result = await client.GetAsync("/Anonymous2");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(Http2Version, result.Version);
    }

    [ConditionalTheory]
    [MemberData(nameof(Http11And2))]
    public async Task Unauthorized_401Negotiate(Version version)
    {
        using var host = await CreateHostAsync();
        // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade. WinHttpHandler does.
        using var client = CreateWinHttpClient(host);
        client.DefaultRequestVersion = version;

        var result = await client.GetAsync("/Unauthorized");
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("Negotiate", result.Headers.WwwAuthenticate.ToString());
        Assert.Equal(Http11Version, result.Version); // HTTP/2 downgrades.
    }

    [ConditionalTheory]
    [MemberData(nameof(Http11And2))]
    public async Task UnauthorizedAfterAuthenticated_Success(Version version)
    {
        using var host = await CreateHostAsync();
        // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade. WinHttpHandler does.
        using var client = CreateWinHttpClient(host);
        client.DefaultRequestVersion = version;

        var result = await client.GetAsync("/Authenticate");
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(Http11Version, result.Version); // HTTP/2 downgrades.

        result = await client.GetAsync("/Unauthorized");
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("Negotiate", result.Headers.WwwAuthenticate.ToString());
        Assert.Equal(Http11Version, result.Version); // HTTP/2 downgrades.
    }

    private Task<IHost> CreateHostAsync(Action<NegotiateOptions> configureOptions = null)
    {
        var builder = new HostBuilder()
            .ConfigureServices(AddTestLogging)
            .ConfigureServices(services => services
                .AddRouting()
                .AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate(configureOptions))
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder.UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 0, endpoint =>
                    {
                        endpoint.UseHttps("negotiateAuthCert.pfx", "testPassword");
                    });
                });
                webHostBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseWebSockets();
                    app.UseEndpoints(ConfigureEndpoints);
                });
            });

        return builder.StartAsync();
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

        builder.Map("/AuthenticateWebSocket", async context =>
        {
            if (!context.User.Identity.IsAuthenticated)
            {
                await context.ChallengeAsync();
                return;
            }

            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            Assert.False(string.IsNullOrEmpty(context.User.Identity.Name), "name");

            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

            await webSocket.SendAsync(Encoding.UTF8.GetBytes("Hello World!"), WebSocketMessageType.Text, endOfMessage: true, context.RequestAborted);
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
    }

    // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade. WinHttpHandler does.
    private static HttpClient CreateWinHttpClient(IHost host)
    {
        var address = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();

        // WinHttpHandler always uses default credentials on localhost
        return new HttpClient(new WinHttpHandler()
        {
            ServerCertificateValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        })
        {
            BaseAddress = new Uri(address)
        };
    }

    // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade. WinHttpHandler does.
    private static HttpClient CreateSocketHttpClient(IHost host, bool useDefaultCredentials = false)
    {
        var address = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.First();

        return new HttpClient(new HttpClientHandler()
        {
            UseDefaultCredentials = useDefaultCredentials,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        })
        {
            BaseAddress = new Uri(address)
        };
    }
}
