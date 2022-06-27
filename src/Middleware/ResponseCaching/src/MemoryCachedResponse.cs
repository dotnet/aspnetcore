// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ResponseCaching;

internal sealed class MemoryCachedResponse
{
    public DateTimeOffset Created { get; set; }

    public int StatusCode { get; set; }

    public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

    public CachedResponseBody Body { get; set; } = default!;
}
