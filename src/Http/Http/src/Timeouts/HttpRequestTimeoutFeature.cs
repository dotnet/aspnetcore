// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Timeouts;

internal sealed class HttpRequestTimeoutFeature : IHttpRequestTimeoutFeature
{
    private readonly CancellationTokenSource _timeoutCancellationTokenSource;

    public HttpRequestTimeoutFeature(CancellationTokenSource timeoutCancellationTokenSource)
    {
        _timeoutCancellationTokenSource = timeoutCancellationTokenSource;
    }

    public CancellationToken RequestTimeoutToken => _timeoutCancellationTokenSource.Token;

    public void DisableTimeout()
    {
        _timeoutCancellationTokenSource.CancelAfter(Timeout.Infinite);
    }
}
