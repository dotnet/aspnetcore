// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Negotiate.Client.Controllers;

[Route("authtest")]
[ApiController]
public class AuthTestController : ControllerBase
{
    private const int StatusCode600WrongStatusCode = 600;
    private const int StatusCode601WrongUser = 601;
    private const int StatusCode602WrongAuthType = 602;
    private const int StatusCode603WrongAuthHeader = 603;
    private const int StatusCode604WrongProtocol = 604;

    private const string Http11Protocol = "HTTP/1.1";
    private const string Http2Protocol = "HTTP/2";

    [HttpGet]
    [Route("Anonymous/Unrestricted")]
    public async Task<IActionResult> AnonymousUnrestricted([FromQuery] string server, [FromQuery] string protocol)
    {
        var client = CreateSocketHttpClient(server);
        client.DefaultRequestVersion = GetProtocolVersion(protocol);

        var result = await client.GetAsync("auth/Unrestricted");
        var body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status200OK, result.StatusCode, body, out var actionResult)
            || HasWrongProtocol(protocol, result.Version, out actionResult)
            || HasUser(body, out actionResult))
        {
            return actionResult;
        }

        return Ok();
    }

    [HttpGet]
    [Route("Anonymous/Authorized")]
    public async Task<IActionResult> AnonymousAuthorized([FromQuery] string server, [FromQuery] string protocol)
    {
        // Note WinHttpHandler cannot disable default credentials on localhost.
        var client = CreateSocketHttpClient(server);
        client.DefaultRequestVersion = GetProtocolVersion(protocol);

        var result = await client.GetAsync("auth/Authorized");
        var body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status401Unauthorized, result.StatusCode, body, out var actionResult)
            || HasWrongProtocol(protocol, result.Version, out actionResult))
        {
            return actionResult;
        }

        var authHeader = result.Headers.WwwAuthenticate.ToString();

        if (!string.Equals("Negotiate", authHeader))
        {
            return StatusCode(StatusCode603WrongAuthHeader, authHeader);
        }

        return Ok();
    }

    [HttpGet]
    [Route("DefaultCredentials/Authorized")]
    public async Task<IActionResult> DefaultCredentialsAuthorized([FromQuery] string server, [FromQuery] string protocol)
    {
        // Note WinHttpHandler cannot disable default credentials on localhost.
        // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade HTTP/2. WinHttpHandler does.
        var client = CreateWinHttpClient(server, useDefaultCredentials: true);
        client.DefaultRequestVersion = GetProtocolVersion(protocol);

        var result = await client.GetAsync("auth/Authorized");
        var body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status200OK, result.StatusCode, body, out var actionResult)
            // Automatic downgrade to HTTP/1.1
            || HasWrongProtocol(Http11Protocol, result.Version, out actionResult)
            || MissingUser(body, out actionResult))
        {
            return actionResult;
        }

        return Ok();
    }

    [HttpGet]
    [Route("AfterAuth/Unrestricted/Persist")]
    public async Task<IActionResult> AfterAuthUnrestrictedPersist([FromQuery] string server, [FromQuery] string protocol1, [FromQuery] string protocol2)
    {
        // Note WinHttpHandler cannot disable default credentials on localhost.
        // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade HTTP/2. WinHttpHandler does.
        var client = CreateWinHttpClient(server, useDefaultCredentials: true);
        client.DefaultRequestVersion = GetProtocolVersion(protocol1);

        var result = await client.GetAsync("auth/Authorized");
        var body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status200OK, result.StatusCode, body, out var actionResult)
            // Automatic downgrade to HTTP/1.1
            || HasWrongProtocol(Http11Protocol, result.Version, out actionResult)
            || MissingUser(body, out actionResult))
        {
            return actionResult;
        }

        result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "auth/Unrestricted") { Version = GetProtocolVersion(protocol2) });
        body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status200OK, result.StatusCode, body, out actionResult)
            || HasWrongProtocol(Http11Protocol, result.Version, out actionResult)
            || MissingUser(body, out actionResult))
        {
            return actionResult;
        }

        return Ok();
    }

    [HttpGet]
    [Route("AfterAuth/Unrestricted/NonPersist")]
    public async Task<IActionResult> AfterAuthUnrestrictedNonPersist([FromQuery] string server, [FromQuery] string protocol1, [FromQuery] string protocol2)
    {
        // Note WinHttpHandler cannot disable default credentials on localhost.
        // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade HTTP/2. WinHttpHandler does.
        var client = CreateWinHttpClient(server, useDefaultCredentials: true);
        client.DefaultRequestVersion = GetProtocolVersion(protocol1);

        var result = await client.GetAsync("auth/Authorized");
        var body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status200OK, result.StatusCode, body, out var actionResult)
            // Automatic downgrade to HTTP/1.1
            || HasWrongProtocol(Http11Protocol, result.Version, out actionResult)
            || MissingUser(body, out actionResult))
        {
            return actionResult;
        }

        result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "auth/Unrestricted") { Version = GetProtocolVersion(protocol2) });
        body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status200OK, result.StatusCode, body, out actionResult)
            || HasWrongProtocol(Http11Protocol, result.Version, out actionResult)
            || HasUser(body, out actionResult))
        {
            return actionResult;
        }

        return Ok();
    }

    [HttpGet]
    [Route("AfterAuth/Authorized/NonPersist")]
    public async Task<IActionResult> AfterAuthAuthorizedNonPersist([FromQuery] string server, [FromQuery] string protocol1, [FromQuery] string protocol2)
    {
        // Note WinHttpHandler cannot disable default credentials on localhost.
        // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade HTTP/2. WinHttpHandler does.
        var client = CreateWinHttpClient(server, useDefaultCredentials: true);
        client.DefaultRequestVersion = GetProtocolVersion(protocol1);

        var result = await client.GetAsync("auth/Authorized");
        var body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status200OK, result.StatusCode, body, out var actionResult)
            // Automatic downgrade to HTTP/1.1
            || HasWrongProtocol(Http11Protocol, result.Version, out actionResult)
            || MissingUser(body, out actionResult))
        {
            return actionResult;
        }

        result = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "auth/Authorized") { Version = GetProtocolVersion(protocol2) });
        body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status200OK, result.StatusCode, body, out actionResult)
            || HasWrongProtocol(Http11Protocol, result.Version, out actionResult)
            || MissingUser(body, out actionResult))
        {
            return actionResult;
        }

        return Ok();
    }

    [HttpGet]
    [Route("Unauthorized")]
    public async Task<IActionResult> Unauthorized([FromQuery] string server, [FromQuery] string protocol)
    {
        var client = CreateWinHttpClient(server, useDefaultCredentials: true);
        client.DefaultRequestVersion = GetProtocolVersion(protocol);

        var result = await client.GetAsync("auth/Unauthorized");
        var body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status401Unauthorized, result.StatusCode, body, out var actionResult)
            || HasWrongProtocol(Http11Protocol, result.Version, out actionResult)) // HTTP/2 downgrades.
        {
            return actionResult;
        }

        var authHeader = result.Headers.WwwAuthenticate.ToString();

        if (!string.Equals("Negotiate", authHeader))
        {
            return StatusCode(StatusCode603WrongAuthHeader, authHeader);
        }

        return Ok();
    }

    [HttpGet]
    [Route("AfterAuth/Unauthorized")]
    public async Task<IActionResult> AfterAuthUnauthorized([FromQuery] string server, [FromQuery] string protocol)
    {
        var client = CreateWinHttpClient(server, useDefaultCredentials: true);
        client.DefaultRequestVersion = GetProtocolVersion(protocol);

        var result = await client.GetAsync("auth/Authorized");
        var body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status200OK, result.StatusCode, body, out var actionResult)
            // Automatic downgrade to HTTP/1.1
            || HasWrongProtocol(Http11Protocol, result.Version, out actionResult)
            || MissingUser(body, out actionResult))
        {
            return actionResult;
        }

        result = await client.GetAsync("auth/Unauthorized");
        body = await result.Content.ReadAsStringAsync();

        if (HasWrongStatusCode(StatusCodes.Status401Unauthorized, result.StatusCode, body, out actionResult)
            || HasWrongProtocol(Http11Protocol, result.Version, out actionResult)) // HTTP/2 downgrades.
        {
            return actionResult;
        }

        var authHeader = result.Headers.WwwAuthenticate.ToString();

        if (!string.Equals("Negotiate", authHeader))
        {
            return StatusCode(StatusCode603WrongAuthHeader, authHeader);
        }

        return Ok();
    }

    private bool HasWrongStatusCode(int expected, HttpStatusCode actual, string body, out IActionResult actionResult)
    {
        if (expected != (int)actual)
        {
            actionResult = StatusCode(StatusCode600WrongStatusCode, $"{actual} {body}");
            return true;
        }
        actionResult = null;
        return false;
    }

    private bool HasWrongProtocol(string expected, Version actual, out IActionResult actionResult)
    {
        if ((expected == Http11Protocol && actual != new Version(1, 1))
            || (expected == Http2Protocol && actual != new Version(2, 0)))
        {
            actionResult = StatusCode(StatusCode604WrongProtocol, actual.ToString());
            return true;
        }
        actionResult = null;
        return false;
    }

    private bool MissingUser(string body, out IActionResult actionResult)
    {
        var details = JsonDocument.Parse(body).RootElement;

        if (string.IsNullOrEmpty(details.GetProperty("name").GetString()))
        {
            actionResult = StatusCode(StatusCode601WrongUser, body);
            return true;
        }

        if (string.IsNullOrEmpty(details.GetProperty("authenticationType").GetString()))
        {
            actionResult = StatusCode(StatusCode602WrongAuthType, body);
            return true;
        }

        actionResult = null;
        return false;
    }

    private bool HasUser(string body, out IActionResult actionResult)
    {
        var details = JsonDocument.Parse(body).RootElement;

        if (!string.IsNullOrEmpty(details.GetProperty("name").GetString()))
        {
            actionResult = StatusCode(StatusCode601WrongUser, body);
            return true;
        }

        if (!string.IsNullOrEmpty(details.GetProperty("authenticationType").GetString()))
        {
            actionResult = StatusCode(StatusCode602WrongAuthType, body);
            return true;
        }

        actionResult = null;
        return false;
    }

    // Normally you'd want to re-use clients, but we want to ensure we have fresh state for each test.

    // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade HTTP/2. WinHttpHandler does.
    private HttpClient CreateSocketHttpClient(string remote, bool useDefaultCredentials = false)
    {
        return new HttpClient(new HttpClientHandler()
        {
            UseDefaultCredentials = useDefaultCredentials,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        })
        {
            BaseAddress = new Uri(remote),
        };
    }

    // https://github.com/dotnet/corefx/issues/35195 SocketHttpHandler won't downgrade HTTP/2. WinHttpHandler does.
    private HttpClient CreateWinHttpClient(string remote, bool useDefaultCredentials = false)
    {
        // WinHttpHandler always uses default credentials on localhost
        return new HttpClient(new WinHttpHandler()
        {
            ServerCredentials = CredentialCache.DefaultCredentials,
            ServerCertificateValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        })
        {
            BaseAddress = new Uri(remote)
        };
    }

    private Version GetProtocolVersion(string protocol)
    {
        switch (protocol)
        {
            case "HTTP/1.1": return new Version(1, 1);
            case "HTTP/2": return new Version(2, 0);
            default: throw new NotImplementedException(Request.Protocol);
        }
    }
}
