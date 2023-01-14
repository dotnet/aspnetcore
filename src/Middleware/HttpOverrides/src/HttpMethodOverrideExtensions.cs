// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for enabling <see cref="HttpMethodOverrideMiddleware"/>.
/// </summary>
public static class HttpMethodOverrideExtensions
{
    /// <summary>
    /// Allows incoming POST request to override method type with type specified in header. This middleware
    /// is used when a client is limited to sending GET or POST methods but wants to invoke other HTTP methods.
    /// By default, the X-HTTP-Method-Override request header is used to specify the HTTP method being tunneled.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    public static IApplicationBuilder UseHttpMethodOverride(this IApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseMiddleware<HttpMethodOverrideMiddleware>();
    }

    /// <summary>
    /// Allows incoming POST request to override method type with type specified in form. This middleware
    /// is used when a client is limited to sending GET or POST methods but wants to invoke other HTTP methods.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/> instance this method extends.</param>
    /// <param name="options">
    /// The <see cref="HttpMethodOverrideOptions"/> which indicates which form type specifies the override method.
    /// </param>
    public static IApplicationBuilder UseHttpMethodOverride(this IApplicationBuilder builder, HttpMethodOverrideOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        return builder.UseMiddleware<HttpMethodOverrideMiddleware>(Options.Create(options));
    }
}
