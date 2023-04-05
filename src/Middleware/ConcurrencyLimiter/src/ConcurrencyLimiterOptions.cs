// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.ConcurrencyLimiter;

/// <summary>
/// Specifies options for the <see cref="ConcurrencyLimiterMiddleware"/>.
/// </summary>
[Obsolete("Concurrency Limiter middleware has been deprecated and will be removed in a future release. Update the app to use concurrency features in rate limiting middleware. For more information, see https://aka.ms/aspnet/rate-limiting")]
public class ConcurrencyLimiterOptions
{
    /// <summary>
    /// A <see cref="RequestDelegate"/> that handles requests rejected by this middleware.
    /// If it doesn't modify the response, an empty 503 response will be written.
    /// </summary>
    public RequestDelegate OnRejected { get; set; } = context =>
    {
        return Task.CompletedTask;
    };
}
