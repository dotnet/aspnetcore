// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Specifies options for the rate limiting middleware.
/// </summary>
public sealed class RateLimitingOptions
{
    // TODO - Provide a default?
    private PartitionedRateLimiter<HttpContext> _limiter = new NoLimiter<HttpContext>();
    private RequestDelegate _onRejected = context =>
    {
        return Task.CompletedTask;
    };

    /// <summary>
    /// Gets the <see cref="PartitionedRateLimiter{TResource}"/>
    /// </summary>
    public PartitionedRateLimiter<HttpContext> Limiter
    {
        get => _limiter;
        set => _limiter = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// A <see cref="RequestDelegate"/> that handles requests rejected by this middleware.
    /// If it doesn't modify the response, an empty 503 response will be written.
    /// </summary>
    public RequestDelegate OnRejected
    {
        get => _onRejected;
        set => _onRejected = value ?? throw new ArgumentNullException(nameof(value));
    }
}
