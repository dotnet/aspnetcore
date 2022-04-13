// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
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
    private Func<HttpContext, RateLimitLease, Task> _onRejected = (context, lease) =>
    {
        return Task.CompletedTask;
    };

    /// <summary>
    /// Gets or sets the <see cref="PartitionedRateLimiter{TResource}"/>
    /// </summary>
    public PartitionedRateLimiter<HttpContext> Limiter
    {
        get => _limiter;
        set => _limiter = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets a <see cref="Func{HttpContext, RateLimitLease, Task}"/> that handles requests rejected by this middleware.
    /// </summary>
    public Func<HttpContext, RateLimitLease, Task> OnRejected
    {
        get => _onRejected;
        set => _onRejected = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the <see cref="HttpStatusCode"/> to set on the response when a request is rejected.
    /// Defaults to <see cref="HttpStatusCode.ServiceUnavailable"/>.
    /// </summary>
    public HttpStatusCode RejectionStatusCode { get; set; } = HttpStatusCode.ServiceUnavailable;
}
