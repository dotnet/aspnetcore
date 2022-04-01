// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.RateLimiting;

/// <summary>
/// Specifies options for the <see cref="RateLimitingMiddleware"/>.
/// </summary>
public class RateLimitingOptions
{
    // TODO - Provide a default?
    private PartitionedRateLimiter<HttpContext>? _limiter;

    /// <summary>
    /// Gets the <see cref="PartitionedRateLimiter{TResource}"/>
    /// </summary>
    public PartitionedRateLimiter<HttpContext>? Limiter
    {
        get => _limiter;
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            _limiter = value;
        }
    }

    /// <summary>
    /// A <see cref="RequestDelegate"/> that handles requests rejected by this middleware.
    /// If it doesn't modify the response, an empty 503 response will be written.
    /// </summary>
    public RequestDelegate OnRejected { get; set; } = context =>
    {
        return Task.CompletedTask;
    };
}
