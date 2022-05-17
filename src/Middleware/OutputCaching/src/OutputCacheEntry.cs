// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.OutputCaching;

/// <inheritdoc />
internal class OutputCacheEntry : IOutputCacheEntry
{
    /// <inheritdoc />
    public DateTimeOffset Created { get; set; }

    /// <inheritdoc />
    public int StatusCode { get; set; }

    /// <inheritdoc />
    public IHeaderDictionary Headers { get; set; } = default!;

    /// <inheritdoc />
    public CachedResponseBody Body { get; set; } = default!;

    /// <inheritdoc />
    public string[]? Tags { get; set; }
}
