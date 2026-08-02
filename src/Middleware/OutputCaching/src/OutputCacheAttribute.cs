// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// Specifies the parameters necessary for setting appropriate headers in output caching.
/// </summary>
/// <remarks>
/// This attribute requires the output cache middleware.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class OutputCacheAttribute : Attribute
{
    // A nullable-int cannot be used as an Attribute parameter.
    // Hence this nullable-int is present to back the Duration property.
    // The same goes for nullable-ResponseCacheLocation and nullable-bool.
    private int? _duration;
    private bool? _noCache;

    private IOutputCachePolicy? _builtPolicy;

    /// <summary>
    /// Gets or sets the duration in seconds for which the response is cached.
    /// </summary>
    public int Duration
    {
        get => _duration ?? 0;
        init => _duration = value;
    }

    /// <summary>
    /// Gets or sets the value which determines whether the response should be cached or not.
    /// When set to <see langword="true"/>, the response won't be cached.
    /// </summary>
    public bool NoStore
    {
        get => _noCache ?? false;
        init => _noCache = value;
    }

    /// <summary>
    /// Gets or sets the query keys to vary by.
    /// </summary>
    public string[]? VaryByQueryKeys { get; init; }

    /// <summary>
    /// Gets or sets the header names to vary by.
    /// </summary>
    public string[]? VaryByHeaderNames { get; init; }

    /// <summary>
    /// Gets or sets the route value names to vary by.
    /// </summary>
    public string[]? VaryByRouteValueNames { get; init; }

    /// <summary>
    /// Gets or sets tags to apply to the output cache.
    /// </summary>
    public string[]? Tags { get; init; }

    /// <summary>
    /// Gets or sets the value of the cache policy name.
    /// </summary>
    public string? PolicyName { get; init; }

    internal IOutputCachePolicy BuildPolicy()
    {
        if (_builtPolicy != null)
        {
            return _builtPolicy;
        }

        OutputCachePolicyBuilder builder;

        if (PolicyName != null)
        {
            // Don't add the default policy if a named one is used as it could already contain it
            builder = new OutputCachePolicyBuilder(excludeDefaultPolicy: true);
            builder.AddPolicy(new NamedPolicy(PolicyName));
        }
        else
        {
            builder = new();
        }

        if (_noCache != null && _noCache.Value)
        {
            builder.NoCache();
        }

        if (VaryByQueryKeys != null)
        {
            builder.SetVaryByQuery(VaryByQueryKeys);
        }

        if (VaryByHeaderNames != null)
        {
            builder.SetVaryByHeader(VaryByHeaderNames);
        }

        if (VaryByRouteValueNames != null)
        {
            builder.SetVaryByRouteValue(VaryByRouteValueNames);
        }

        if (Tags != null)
        {
            builder.Tag(Tags);
        }

        if (_duration != null)
        {
            builder.Expire(TimeSpan.FromSeconds(_duration.Value));
        }

        return _builtPolicy = builder.Build();
    }
}
