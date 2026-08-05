// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add the well-known passkey
/// endpoints document.
/// </summary>
public static partial class PasskeyEndpointsEndpointRouteBuilderExtensions
{
    internal const string LoggerCategory = "Microsoft.AspNetCore.Identity.PasskeyEndpoints";

    private const string WellKnownPath = "/.well-known/passkey-endpoints";

    /// <summary>
    /// Adds an endpoint that serves the well-known passkey endpoints document at
    /// <c>/.well-known/passkey-endpoints</c>, which advertises where a user can create and manage
    /// passkeys for the application.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the endpoint to.</param>
    /// <returns>An <see cref="IEndpointConventionBuilder"/> to further customize the added endpoint.</returns>
    /// <remarks>
    /// <para>
    /// Credential managers fetch this document to discover whether a site supports passkeys and
    /// where a user can create or manage them, which lets them offer to upgrade a saved password to
    /// a passkey without the user having to visit the site and find the relevant page.
    /// </para>
    /// <para>
    /// The advertised locations are configured with
    /// <see cref="PasskeyEndpointsServiceCollectionExtensions.AddPasskeyEndpoints(IServiceCollection, Action{PasskeyEndpointsOptions})"/>.
    /// If none are configured, an empty document is served, which the specification defines as
    /// signalling support for passkeys without advertising specific pages.
    /// </para>
    /// <para>
    /// The endpoint allows anonymous requests, because credential managers fetch the document
    /// without a user session and the specification does not allow a redirect to be returned.
    /// </para>
    /// <para>
    /// The specification requires the document to be served from the root of the origin, so the
    /// endpoint cannot be added to a route group with a prefix. Doing so throws an
    /// <see cref="InvalidOperationException"/> when the application's endpoints are built, which is
    /// at startup for a typical application.
    /// </para>
    /// <para>
    /// See <see href="https://w3c.github.io/webappsec-passkey-endpoints/"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// The following example advertises the passkey management page of an application:
    /// <code>
    /// builder.Services.AddPasskeyEndpoints(options =>
    /// {
    ///     options.Enroll = "/Account/Manage/Passkeys";
    ///     options.Manage = "/Account/Manage/Passkeys";
    /// });
    ///
    /// var app = builder.Build();
    ///
    /// app.MapWellKnownPasskeyEndpoints();
    /// </code>
    /// A request to <c>https://contoso.com/.well-known/passkey-endpoints</c> then responds with:
    /// <code>
    /// {
    ///   "enroll": "https://contoso.com/Account/Manage/Passkeys",
    ///   "manage": "https://contoso.com/Account/Manage/Passkeys"
    /// }
    /// </code>
    /// </example>
    public static IEndpointConventionBuilder MapWellKnownPasskeyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var services = endpoints.ServiceProvider;
        var options = services.GetRequiredService<IOptions<PasskeyEndpointsOptions>>().Value;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(LoggerCategory);

        // These values often come from configuration, so they are normalized once here rather than
        // trusted verbatim on every request.
        var enroll = Normalize(options.Enroll);
        var manage = Normalize(options.Manage);
        var prfUsageDetails = Normalize(options.PrfUsageDetails);

        var serverDomain = services.GetRequiredService<IOptions<IdentityPasskeyOptions>>().Value.ServerDomain;

        // Credential managers poll this document, so the diagnostic is written at most once.
        var serverDomainLogged = 0;

        var routeBuilder = endpoints
            .MapMethods(WellKnownPath, [HttpMethods.Get, HttpMethods.Head], (HttpContext context) =>
            {
                var request = context.Request;

                if (serverDomain is not null
                    && !IsServedByServerDomain(serverDomain, request.Host.Host)
                    && Interlocked.Exchange(ref serverDomainLogged, 1) == 0)
                {
                    Log.PasskeyEndpointsServerDomainMismatch(logger, serverDomain, request.Host.Host);
                }

                var response = new PasskeyEndpointsResponse
                {
                    Enroll = ResolveUrl(enroll, request),
                    Manage = ResolveUrl(manage, request),
                    PrfUsageDetails = ResolveUrl(prfUsageDetails, request),
                };

                return TypedResults.Json(
                    response,
                    IdentityEndpointsJsonSerializerContext.Default.PasskeyEndpointsResponse);
            })
            .AllowAnonymous();

        // A route group prefix would move the document off the root of the origin, where no
        // credential manager will look for it. Failing at startup is better than serving a document
        // that is never found.
        routeBuilder.Add(endpointBuilder =>
        {
            var pattern = ((RouteEndpointBuilder)endpointBuilder).RoutePattern.RawText;

            if (!string.Equals(pattern, WellKnownPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"The well-known passkey endpoints document must be served from '{WellKnownPath}', but it was " +
                    $"mapped to '{pattern}'. Call {nameof(MapWellKnownPasskeyEndpoints)} directly on the application " +
                    "rather than on a route group with a prefix.");
            }
        });

        return routeBuilder;
    }

    private static string? Normalize(string? value)
    {
        // A value that is entirely whitespace is treated as unset, so that it is omitted from the
        // document instead of being advertised as a URL pointing at nothing.
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Uri already ignores surrounding whitespace when parsing an absolute URL, so trimming here
        // gives relative values the same treatment and keeps stray whitespace out of the document.
        return value.Trim();
    }

    private static bool IsServedByServerDomain(string serverDomain, string host)
    {
        // The relying party identifier may be a registrable suffix of the origin, so a request to a
        // subdomain of it is a correct configuration rather than something to warn about.
        if (host.Equals(serverDomain, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return host.Length > serverDomain.Length
            && host[host.Length - serverDomain.Length - 1] == '.'
            && host.EndsWith(serverDomain, StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveUrl(string? value, HttpRequest request)
    {
        if (value is null)
        {
            return null;
        }

        // Absolute URLs are advertised after normalization, which allows pointing at a separate
        // host. They are normalized rather than used verbatim because Uri accepts values that are
        // not well formed, such as one containing a raw space. Only http and https qualify: every
        // other scheme parses too, and on Unix a rooted path such as "/account/passkeys" is a valid
        // implicit file URI, so an unrestricted check would advertise it unresolved on those
        // platforms.
        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.AbsoluteUri;
        }

        // Relative values are resolved against the current request. They are deliberately not
        // resolved against the relying party identifier, which may be a registrable suffix of the
        // origin serving these pages.
        var path = value;
        var fragment = FragmentString.Empty;
        var query = QueryString.Empty;

        // The query and fragment are separated first, because PathString escapes '?' and '#' as part
        // of the path.
        var fragmentIndex = path.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            fragment = new FragmentString(path[fragmentIndex..]);
            path = path[..fragmentIndex];
        }

        var queryIndex = path.IndexOf('?');
        if (queryIndex >= 0)
        {
            query = new QueryString(path[queryIndex..]);
            path = path[..queryIndex];
        }

        if (!path.StartsWith('/'))
        {
            path = $"/{path}";
        }

        var resolved = UriHelper.BuildAbsolute(
            request.Scheme,
            request.Host,
            request.PathBase,
            new PathString(path),
            query,
            fragment);

        // Unlike PathString, QueryString and FragmentString assume their value is already escaped,
        // so the result is normalized the same way an absolute value is. Without this a query such
        // as "?return=/my page" would put a raw space in the document.
        return Uri.TryCreate(resolved, UriKind.Absolute, out var uri) ? uri.AbsoluteUri : resolved;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Warning,
            "The well-known passkey endpoints document is being served for the host '{Host}', which does not " +
            "match the configured relying party identifier '{ServerDomain}'. Credential managers only request " +
            "the document at the relying party's origin, so it will not be found unless it is also served there.",
            EventName = "PasskeyEndpointsServerDomainMismatch")]
        public static partial void PasskeyEndpointsServerDomainMismatch(ILogger logger, string serverDomain, string host);
    }
}
