// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Serves the well-known passkey endpoints document at <c>/.well-known/passkey-endpoints</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is a startup filter rather than an endpoint because a library cannot add an endpoint
/// without the application calling <c>Map</c>, and the document is meant to appear automatically.
/// </para>
/// <para>
/// Startup filters run in service registration order, so this middleware runs after the forwarded
/// headers middleware registered by the host, and therefore observes the real client scheme and
/// host. It also runs before <c>UsePathBase</c> and <c>UseRouting</c>, so the document is always
/// served at the root of the origin, where the specification requires it, and cannot be moved by a
/// route group prefix.
/// </para>
/// </remarks>
internal sealed partial class PasskeyEndpointsStartupFilter : IStartupFilter
{
    internal const string LoggerCategory = "Microsoft.AspNetCore.Identity.PasskeyEndpoints";

    private static readonly PathString _path = new("/.well-known/passkey-endpoints");

    private readonly PasskeyEndpointsOptions _options;
    private readonly string? _serverDomain;
    private readonly ILogger _logger;

    public PasskeyEndpointsStartupFilter(
        IOptions<PasskeyEndpointsOptions> options,
        IOptions<IdentityPasskeyOptions> passkeyOptions,
        ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _serverDomain = passkeyOptions.Value.ServerDomain;
        _logger = loggerFactory.CreateLogger(LoggerCategory);
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        // These values often come from configuration, so they are normalized once at startup rather
        // than trusted verbatim on every request.
        var enroll = Normalize(_options.Enroll);
        var manage = Normalize(_options.Manage);

        // Advertising nothing at all would claim passkey support without giving a credential
        // manager anywhere to send the user, so the document is not served in that case.
        if (enroll is null && manage is null)
        {
            Log.NoPasskeyEndpointsConfigured(_logger);
            return next;
        }

        var serverDomain = _serverDomain;
        var logger = _logger;

        // Credential managers poll this document, so the diagnostic is written at most once.
        var serverDomainLogged = 0;

        return builder =>
        {
            builder.Use(async (context, nextMiddleware) =>
            {
                var request = context.Request;

                // Restricted to safe methods so that an application's own handler for another
                // method at this path still runs, matching how static files behave.
                if (!request.Path.Equals(_path, StringComparison.OrdinalIgnoreCase)
                    || !(HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)))
                {
                    await nextMiddleware(context);
                    return;
                }

                if (serverDomain is not null
                    && !string.Equals(serverDomain, request.Host.Host, StringComparison.OrdinalIgnoreCase)
                    && Interlocked.Exchange(ref serverDomainLogged, 1) == 0)
                {
                    Log.PasskeyEndpointsServerDomainMismatch(logger, serverDomain, request.Host.Host);
                }

                var response = new PasskeyEndpointsResponse
                {
                    Enroll = ResolveUrl(enroll, request),
                    Manage = ResolveUrl(manage, request),
                };

                await context.Response.WriteAsJsonAsync(
                    response,
                    IdentityEndpointsJsonSerializerContext.Default.PasskeyEndpointsResponse);
            });

            next(builder);
        };
    }

    private static string? Normalize(string? value)
    {
        // A value that is entirely whitespace is treated as unset, so that it triggers the
        // unconfigured warning instead of being advertised as a URL pointing at nothing.
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Uri already ignores surrounding whitespace when parsing an absolute URL, so trimming here
        // gives relative values the same treatment and keeps stray whitespace out of the document.
        return value.Trim();
    }

    private static string? ResolveUrl(string? value, HttpRequest request)
    {
        if (value is null)
        {
            return null;
        }

        // Absolute URLs are advertised unchanged, which allows pointing at a separate host.
        if (Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            return value;
        }

        // Relative values are resolved against the current request. They are deliberately not
        // resolved against the relying party identifier, which may be a registrable suffix of the
        // origin serving these pages.
        var path = value.StartsWith('/') ? value : $"/{value}";

        return $"{request.Scheme}://{request.Host.ToUriComponent()}{request.PathBase.ToUriComponent()}{path}";
    }

    private static partial class Log
    {
        [LoggerMessage(0, LogLevel.Warning,
            "Passkey endpoints were added, but neither a passkey creation page nor a passkey management " +
            "page was configured. The well-known passkey endpoints document will not be served. Configure " +
            "'PasskeyEndpointsOptions.Enroll' or 'PasskeyEndpointsOptions.Manage' to advertise them.",
            EventName = "NoPasskeyEndpointsConfigured")]
        public static partial void NoPasskeyEndpointsConfigured(ILogger logger);

        [LoggerMessage(1, LogLevel.Warning,
            "The well-known passkey endpoints document is being served for the host '{Host}', which does not " +
            "match the configured relying party identifier '{ServerDomain}'. Credential managers only request " +
            "the document at the relying party's origin, so it will not be found unless it is also served there.",
            EventName = "PasskeyEndpointsServerDomainMismatch")]
        public static partial void PasskeyEndpointsServerDomainMismatch(ILogger logger, string serverDomain, string host);
    }
}
