// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener;

public class AuthenticationOnExistingQueueTests_Attach : AuthenticationOnExistingQueueTests
{
    protected override string ConfigureServer(HttpSysOptions options, string baseServerAddress)
    {
        options.RequestQueueMode = RequestQueueMode.Attach;
        return baseServerAddress;
    }
}

public class AuthenticationOnExistingQueueTests_CreateOrAttach_UseExistingUrlPrefix : AuthenticationOnExistingQueueTests
{
    protected override string ConfigureServer(HttpSysOptions options, string baseServerAddress)
    {
        options.RequestQueueMode = RequestQueueMode.CreateOrAttach;
        return baseServerAddress;
    }
}

public class AuthenticationOnExistingQueueTests_CreateOrAttach_UseNewUrlPrefix : AuthenticationOnExistingQueueTests
{
    protected override string ConfigureServer(HttpSysOptions options, string baseServerAddress)
    {
        options.RequestQueueMode = RequestQueueMode.CreateOrAttach;
        var basePrefix = UrlPrefix.Create(baseServerAddress);
        var prefix = UrlPrefix.Create(basePrefix.Scheme, basePrefix.Host, basePrefix.Port, "/server");
        options.UrlPrefixes.Add(prefix);
        return prefix.ToString();
    }
}

public abstract class AuthenticationOnExistingQueueTests
{
    private static readonly bool AllowAnoymous = true;
    private static readonly bool DenyAnoymous = false;

    [ConditionalTheory]
    [InlineData(AuthenticationSchemes.None)]
    [InlineData(AuthenticationSchemes.Negotiate)]
    [InlineData(AuthenticationSchemes.NTLM)]
    // [InlineData(AuthenticationSchemes.Digest)]
    [InlineData(AuthenticationSchemes.Basic)]
    [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
    public async Task AuthTypes_AllowAnonymous_NoChallenge(AuthenticationSchemes authType)
    {
        using var baseServer = CreateHttpAuthServer(authType, AllowAnoymous);
        using var server = CreateServerOnExistingQueue(authType, AllowAnoymous, baseServer, out var address);

        Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        Assert.NotNull(context.User);
        Assert.False(context.User.Identity.IsAuthenticated);
        Assert.Equal(authType, context.Response.AuthenticationChallenges);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(response.Headers.WwwAuthenticate);
    }

    [ConditionalTheory]
    [InlineData(AuthenticationSchemes.Negotiate)]
    [InlineData(AuthenticationSchemes.NTLM)]
    // [InlineData(AuthenticationType.Digest)] // TODO: Not implemented
    [InlineData(AuthenticationSchemes.Basic)]
    public async Task AuthType_RequireAuth_ChallengesAdded(AuthenticationSchemes authType)
    {
        using var baseServer = CreateHttpAuthServer(authType, DenyAnoymous);
        using var server = CreateServerOnExistingQueue(authType, DenyAnoymous, baseServer, out var address);

        Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

        var contextTask = server.AcceptAsync(Utilities.DefaultTimeout); // Fails when the server shuts down, the challenge happens internally.
        var response = await responseTask;
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    [ConditionalTheory]
    [InlineData(AuthenticationSchemes.Negotiate)]
    [InlineData(AuthenticationSchemes.NTLM)]
    // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
    [InlineData(AuthenticationSchemes.Basic)]
    public async Task AuthType_AllowAnonymousButSpecify401_ChallengesAdded(AuthenticationSchemes authType)
    {
        using var baseServer = CreateHttpAuthServer(authType, AllowAnoymous);
        using var server = CreateServerOnExistingQueue(authType, AllowAnoymous, baseServer, out var address);

        Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        Assert.NotNull(context.User);
        Assert.False(context.User.Identity.IsAuthenticated);
        Assert.Equal(authType, context.Response.AuthenticationChallenges);
        context.Response.StatusCode = 401;
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    [ConditionalFact]
    public async Task MultipleAuthTypes_AllowAnonymousButSpecify401_ChallengesAdded()
    {
        AuthenticationSchemes authType =
            AuthenticationSchemes.Negotiate
            | AuthenticationSchemes.NTLM
            /* | AuthenticationSchemes.Digest TODO: Not implemented */
            | AuthenticationSchemes.Basic;
        using var baseServer = CreateHttpAuthServer(authType, AllowAnoymous);
        using var server = CreateServerOnExistingQueue(authType, AllowAnoymous, baseServer, out var address);

        Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        Assert.NotNull(context.User);
        Assert.False(context.User.Identity.IsAuthenticated);
        Assert.Equal(authType, context.Response.AuthenticationChallenges);
        context.Response.StatusCode = 401;
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Negotiate, NTLM, basic", response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
    }

    [ConditionalTheory]
    [InlineData(AuthenticationSchemes.Negotiate)]
    [InlineData(AuthenticationSchemes.NTLM)]
    // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
    // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
    [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationType.Digest |*/ AuthenticationSchemes.Basic)]
    public async Task AuthTypes_AllowAnonymousButSpecify401_Success(AuthenticationSchemes authType)
    {
        using var baseServer = CreateHttpAuthServer(authType, AllowAnoymous);
        using var server = CreateServerOnExistingQueue(authType, AllowAnoymous, baseServer, out var address);

        Task<HttpResponseMessage> responseTask = SendRequestAsync(address, useDefaultCredentials: true);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        Assert.NotNull(context.User);
        Assert.False(context.User.Identity.IsAuthenticated);
        Assert.Equal(authType, context.Response.AuthenticationChallenges);
        context.Response.StatusCode = 401;
        context.Dispose();

        context = await server.AcceptAsync(Utilities.DefaultTimeout);
        Assert.NotNull(context.User);
        Assert.True(context.User.Identity.IsAuthenticated);
        Assert.Equal(authType, context.Response.AuthenticationChallenges);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [ConditionalTheory]
    [InlineData(AuthenticationSchemes.Negotiate)]
    [InlineData(AuthenticationSchemes.NTLM)]
    // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
    // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
    [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationType.Digest |*/ AuthenticationSchemes.Basic)]
    public async Task AuthTypes_RequireAuth_Success(AuthenticationSchemes authType)
    {
        using var baseServer = CreateHttpAuthServer(authType, DenyAnoymous);
        using var server = CreateServerOnExistingQueue(authType, DenyAnoymous, baseServer, out var address);

        Task<HttpResponseMessage> responseTask = SendRequestAsync(address, useDefaultCredentials: true);

        var context = await server.AcceptAsync(Utilities.DefaultTimeout);
        Assert.NotNull(context.User);
        Assert.True(context.User.Identity.IsAuthenticated);
        Assert.Equal(authType, context.Response.AuthenticationChallenges);
        context.Dispose();

        var response = await responseTask;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    protected abstract string ConfigureServer(HttpSysOptions options, string baseServerAddress);

    private HttpSysListener CreateHttpAuthServer(AuthenticationSchemes authType, bool allowAnonymous)
    {
        var server = Utilities.CreateDynamicHttpServer("/baseServer", out var root, out var baseAddress);
        server.Options.Authentication.Schemes = authType;
        server.Options.Authentication.AllowAnonymous = allowAnonymous;
        return server;
    }

    private HttpSysListener CreateServerOnExistingQueue(AuthenticationSchemes authScheme, bool allowAnonymos, HttpSysListener baseServer, out string address)
    {
        string serverAddress = null;
        var baseServerAddress = baseServer.Options.UrlPrefixes.First().ToString();
        var server = Utilities.CreateServer(options =>
        {
            options.RequestQueueName = baseServer.Options.RequestQueueName;
            options.Authentication.Schemes = authScheme;
            options.Authentication.AllowAnonymous = allowAnonymos;
            serverAddress = ConfigureServer(options, baseServerAddress);
        });

        address = serverAddress;
        return server;
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool useDefaultCredentials = false)
    {
        HttpClientHandler handler = new HttpClientHandler();
        handler.UseDefaultCredentials = useDefaultCredentials;
        using HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
        return await client.GetAsync(uri);
    }
}
