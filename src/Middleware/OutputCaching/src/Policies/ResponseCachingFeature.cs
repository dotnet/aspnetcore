// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.ResponseCaching;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// The <see cref="IResponseCachingFeature"/> provided by <see cref="ResponseCachingPolicy"/>, so applications setting
/// <see cref="IResponseCachingFeature.VaryByQueryKeys"/> (e.g. MVC's <c>[ResponseCache(VaryByQueryKeys = ...)]</c>) keep working.
/// </summary>
internal sealed class ResponseCachingFeature : IResponseCachingFeature
{
    private readonly OutputCacheContext _context;
    private string[]? _varyByQueryKeys;

    public ResponseCachingFeature(OutputCacheContext context)
    {
        _context = context;
    }

    public string[]? VaryByQueryKeys
    {
        get => _varyByQueryKeys;
        set
        {
            // Same validation as the Response Caching middleware's feature.
            if (value?.Length > 1)
            {
                foreach (var queryKey in value)
                {
                    if (string.IsNullOrEmpty(queryKey))
                    {
                        throw new ArgumentException($"When {nameof(value)} contains more than one value, it cannot contain a null or empty value.", nameof(value));
                    }
                }
            }

            _varyByQueryKeys = value;

            if (!_context.ResponseStarted)
            {
                VaryByQueryKeysWhenResponseStarted = value;
            }
        }
    }

    /// <summary>
    /// The query keys set before the response started. Like Response Caching, which reads them when the response starts,
    /// the query keys set later aren't part of the vary rules.
    /// </summary>
    internal string[]? VaryByQueryKeysWhenResponseStarted { get; private set; }
}
