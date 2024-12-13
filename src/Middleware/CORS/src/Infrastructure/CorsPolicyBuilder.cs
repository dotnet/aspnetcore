// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.Cors.Infrastructure;

/// <summary>
/// Exposes methods to build a policy.
/// </summary>
public class CorsPolicyBuilder
{
    private readonly CorsPolicy _policy = new CorsPolicy();

    /// <summary>
    /// Creates a new instance of the <see cref="CorsPolicyBuilder"/>.
    /// </summary>
    /// <param name="origins">list of origins which can be added.</param>
    /// <remarks> <see cref="WithOrigins(string[])"/> for details on normalizing the origin value.</remarks>
    public CorsPolicyBuilder(params string[] origins)
    {
        WithOrigins(origins);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="CorsPolicyBuilder"/>.
    /// </summary>
    /// <param name="policy">The policy which will be used to intialize the builder.</param>
    public CorsPolicyBuilder(CorsPolicy policy)
    {
        Combine(policy);
    }

    /// <summary>
    /// Adds the specified <paramref name="origins"/> to the policy.
    /// </summary>
    /// <param name="origins">The origins that are allowed.</param>
    /// <returns>The current policy builder.</returns>
    /// <remarks>
    /// This method normalizes the origin value prior to adding it to <see cref="CorsPolicy.Origins"/> to match
    /// the normalization performed by the browser on the value sent in the <c>ORIGIN</c> header.
    /// <list type="bullet">
    /// <item>
    /// <description>If the specified origin has an internationalized domain name (IDN), the punycoded value is used. If the origin
    /// specifies a default port (e.g. 443 for HTTPS or 80 for HTTP), this will be dropped as part of normalization.
    /// Finally, the scheme and punycoded host name are culture invariant lower cased before being added to the <see cref="CorsPolicy.Origins"/>
    /// collection.</description>
    /// </item>
    /// <item>
    /// <description>For all other origins, normalization involves performing a culture invariant lower casing of the host name.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public CorsPolicyBuilder WithOrigins(params string[] origins)
    {
        ArgumentNullException.ThrowIfNull(origins);

        foreach (var origin in origins)
        {
            var normalizedOrigin = GetNormalizedOrigin(origin);
            _policy.Origins.Add(normalizedOrigin);
        }

        return this;
    }

    internal static string GetNormalizedOrigin(string origin)
    {
        ArgumentNullException.ThrowIfNull(origin);

        if (Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            !string.Equals(uri.IdnHost, uri.Host, StringComparison.Ordinal))
        {
            var builder = new UriBuilder(uri.Scheme.ToLowerInvariant(), uri.IdnHost.ToLowerInvariant());
            if (!uri.IsDefaultPort)
            {
                // Uri does not have a way to differentiate between a port value inferred by default (e.g. Port = 80 for http://www.example.com) and
                // a default port value that is specified (e.g. Port = 80 for http://www.example.com:80). Although the HTTP or FETCH spec does not say
                // anything about including the default port as part of the Origin header, at the time of writing, browsers drop "default" port when navigating
                // and when sending the Origin header. All this goes to say, it appears OK to drop an explicitly specified port,
                // if it is the default port when working with an IDN host.
                builder.Port = uri.Port;
            }

            return builder.Uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
        }

        return origin.ToLowerInvariant();
    }

    /// <summary>
    /// Adds the specified <paramref name="headers"/> to the policy.
    /// </summary>
    /// <param name="headers">The headers which need to be allowed in the request.</param>
    /// <returns>The current policy builder.</returns>
    public CorsPolicyBuilder WithHeaders(params string[] headers)
    {
        foreach (var req in headers)
        {
            _policy.Headers.Add(req);
        }
        return this;
    }

    /// <summary>
    /// Adds the specified <paramref name="exposedHeaders"/> to the policy.
    /// </summary>
    /// <param name="exposedHeaders">The headers which need to be exposed to the client.</param>
    /// <returns>The current policy builder.</returns>
    public CorsPolicyBuilder WithExposedHeaders(params string[] exposedHeaders)
    {
        foreach (var req in exposedHeaders)
        {
            _policy.ExposedHeaders.Add(req);
        }

        return this;
    }

    /// <summary>
    /// Adds the specified <paramref name="methods"/> to the policy.
    /// </summary>
    /// <param name="methods">The methods which need to be added to the policy.</param>
    /// <returns>The current policy builder.</returns>
    public CorsPolicyBuilder WithMethods(params string[] methods)
    {
        foreach (var req in methods)
        {
            _policy.Methods.Add(req);
        }

        return this;
    }

    /// <summary>
    /// Sets the policy to allow credentials.
    /// </summary>
    /// <returns>The current policy builder.</returns>
    public CorsPolicyBuilder AllowCredentials()
    {
        _policy.SupportsCredentials = true;
        return this;
    }

    /// <summary>
    /// Sets the policy to not allow credentials.
    /// </summary>
    /// <returns>The current policy builder.</returns>
    public CorsPolicyBuilder DisallowCredentials()
    {
        _policy.SupportsCredentials = false;
        return this;
    }

    /// <summary>
    /// Ensures that the policy allows any origin.
    /// </summary>
    /// <returns>The current policy builder.</returns>
    public CorsPolicyBuilder AllowAnyOrigin()
    {
        _policy.Origins.Clear();
        _policy.Origins.Add(CorsConstants.AnyOrigin);
        return this;
    }

    /// <summary>
    /// Ensures that the policy allows any method.
    /// </summary>
    /// <returns>The current policy builder.</returns>
    public CorsPolicyBuilder AllowAnyMethod()
    {
        _policy.Methods.Clear();
        _policy.Methods.Add(CorsConstants.AnyMethod);
        return this;
    }

    /// <summary>
    /// Ensures that the policy allows any header.
    /// </summary>
    /// <returns>The current policy builder.</returns>
    public CorsPolicyBuilder AllowAnyHeader()
    {
        _policy.Headers.Clear();
        _policy.Headers.Add(CorsConstants.AnyHeader);
        return this;
    }

    /// <summary>
    /// Sets the preflightMaxAge for the underlying policy.
    /// </summary>
    /// <param name="preflightMaxAge">A positive <see cref="TimeSpan"/> indicating the time a preflight
    /// request can be cached.</param>
    /// <returns>The current policy builder.</returns>
    public CorsPolicyBuilder SetPreflightMaxAge(TimeSpan preflightMaxAge)
    {
        _policy.PreflightMaxAge = preflightMaxAge;
        return this;
    }

    /// <summary>
    /// Sets the specified <paramref name="isOriginAllowed"/> for the underlying policy.
    /// </summary>
    /// <param name="isOriginAllowed">The function used by the policy to evaluate if an origin is allowed.</param>
    /// <returns>The current policy builder.</returns>
    public CorsPolicyBuilder SetIsOriginAllowed(Func<string, bool> isOriginAllowed)
    {
        _policy.IsOriginAllowed = isOriginAllowed;
        return this;
    }

    /// <summary>
    /// Sets the <see cref="CorsPolicy.IsOriginAllowed"/> property of the policy to be a function
    /// that allows origins to match a configured wildcarded domain when evaluating if the
    /// origin is allowed.
    /// </summary>
    /// <returns>The current policy builder.</returns>
    public CorsPolicyBuilder SetIsOriginAllowedToAllowWildcardSubdomains()
    {
        _policy.IsOriginAllowed = _policy.IsOriginAnAllowedSubdomain;
        return this;
    }

    /// <summary>
    /// Builds a new <see cref="CorsPolicy"/> using the entries added.
    /// </summary>
    /// <returns>The constructed <see cref="CorsPolicy"/>.</returns>
    public CorsPolicy Build()
    {
        if (_policy.AllowAnyOrigin && _policy.SupportsCredentials)
        {
            throw new InvalidOperationException(Resources.InsecureConfiguration);
        }

        return _policy;
    }

    /// <summary>
    /// Combines the given <paramref name="policy"/> to the existing properties in the builder.
    /// </summary>
    /// <param name="policy">The policy which needs to be combined.</param>
    /// <returns>The current policy builder.</returns>
    private CorsPolicyBuilder Combine(CorsPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        WithOrigins(policy.Origins.ToArray());
        WithHeaders(policy.Headers.ToArray());
        WithExposedHeaders(policy.ExposedHeaders.ToArray());
        WithMethods(policy.Methods.ToArray());
        SetIsOriginAllowed(policy.IsOriginAllowed);

        if (policy.PreflightMaxAge.HasValue)
        {
            SetPreflightMaxAge(policy.PreflightMaxAge.Value);
        }

        if (policy.SupportsCredentials)
        {
            AllowCredentials();
        }
        else
        {
            DisallowCredentials();
        }

        return this;
    }
}
