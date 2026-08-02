// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Cors;

/// <summary>
/// An <see cref="ICorsAuthorizationFilter"/> which ensures that an action does not run for a pre-flight request.
/// </summary>
internal sealed class DisableCorsAuthorizationFilter : ICorsAuthorizationFilter
{
    /// <inheritdoc />
    // Since clients' preflight requests would not have data to authenticate requests, this
    // filter must run before any other authorization filters.
    public int Order => int.MinValue + 100;

    /// <inheritdoc />
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var accessControlRequestMethod =
                    context.HttpContext.Request.Headers[CorsConstants.AccessControlRequestMethod];
        if (string.Equals(
                context.HttpContext.Request.Method,
                CorsConstants.PreflightHttpMethod,
                StringComparison.OrdinalIgnoreCase) &&
            !StringValues.IsNullOrEmpty(accessControlRequestMethod))
        {
            // Short circuit if the request is preflight as that should not result in action execution.
            context.Result = new StatusCodeResult(StatusCodes.Status204NoContent);
        }

        // Let the action be executed.
        return Task.CompletedTask;
    }
}
