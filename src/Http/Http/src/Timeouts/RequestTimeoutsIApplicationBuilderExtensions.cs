// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Timeouts;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for the request timeouts middleware.
/// </summary>
public static class RequestTimeoutsIApplicationBuilderExtensions
{
    /// <summary>
    /// Enables request timeouts for the application.
    /// No timeouts are configured by default, 
    /// they must be configured in <see cref="RequestTimeoutOptions"/>,
    /// the <see cref="RequestTimeoutAttribute"/> on endpoints, or
    /// using the WithRequestTimeout routing extensions.
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseRequestTimeouts(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestTimeoutsMiddleware>();
    }
}
