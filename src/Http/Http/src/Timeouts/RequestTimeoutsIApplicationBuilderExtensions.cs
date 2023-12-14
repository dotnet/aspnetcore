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
    /// <para>
    /// No timeouts are configured by default. They must be configured in <see cref="RequestTimeoutOptions"/>,
    /// the <see cref="RequestTimeoutAttribute"/> on endpoints, or using the WithRequestTimeout routing extensions.
    /// </para>
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    public static IApplicationBuilder UseRequestTimeouts(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestTimeoutsMiddleware>();
    }
}
