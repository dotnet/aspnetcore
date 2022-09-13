// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// When applied, the cached content will be different for every value of the provided headers.
/// </summary>
internal sealed class VaryByHeaderPolicy : IOutputCachePolicy
{
    private readonly StringValues _headerNames;

    private VaryByHeaderPolicy()
    {
    }

    public VaryByHeaderPolicy(string header, params string[] headerNames)
    {
        ArgumentNullException.ThrowIfNull(header);

        _headerNames = header;

        if (headerNames != null && headerNames.Length > 0)
        {
            _headerNames = StringValues.Concat(_headerNames, headerNames);
        }
    }

    public VaryByHeaderPolicy(string[] headerNames)
    {
        ArgumentNullException.ThrowIfNull(headerNames);

        _headerNames = headerNames;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.CacheRequestAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        // No vary by header?
        if (_headerNames.Count == 0)
        {
            context.CacheVaryByRules.HeaderNames = _headerNames;
            return ValueTask.CompletedTask;
        }

        context.CacheVaryByRules.HeaderNames = StringValues.Concat(context.CacheVaryByRules.HeaderNames, _headerNames);

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask IOutputCachePolicy.ServeResponseAsync(OutputCacheContext context, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
