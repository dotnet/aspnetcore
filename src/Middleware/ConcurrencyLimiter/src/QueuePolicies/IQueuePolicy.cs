// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.ConcurrencyLimiter;

/// <summary>
/// Queueing policies, meant to be used with the <see cref="ConcurrencyLimiterMiddleware"></see>.
/// </summary>
public interface IQueuePolicy
{
    /// <summary>
    /// Called for every incoming request.
    /// When it returns 'true' the request procedes to the server.
    /// When it returns 'false' the request is rejected immediately.
    /// </summary>
    ValueTask<bool> TryEnterAsync();

    /// <summary>
    /// Called after successful requests have been returned from the server.
    /// Does NOT get called for rejected requests.
    /// </summary>
    void OnExit();
}
