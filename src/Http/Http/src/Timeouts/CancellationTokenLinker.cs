// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Http.Timeouts;

internal sealed class CancellationTokenLinker : ICancellationTokenLinker
{
    private readonly CancellationTokenSourcePool _ctsPool = new();

    public (CancellationTokenSource linkedCts, CancellationTokenSource timeoutCts) GetLinkedCancellationTokenSource(HttpContext httpContext, CancellationToken originalToken, TimeSpan timeSpan)
    {
        var timeoutCts = _ctsPool.Rent();
        timeoutCts.CancelAfter(timeSpan);
        httpContext.Response.RegisterForDispose(timeoutCts);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(originalToken, timeoutCts.Token);
        return (linkedCts, timeoutCts);
    }
}
