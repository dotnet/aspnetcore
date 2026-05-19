// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ActionResult"/> that when executed will produce a response with content.
/// </summary>
public class ContentResult : ActionResult, IStatusCodeActionResult
{
    /// <summary>
    /// Gets or set the content representing the body of the response.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the Content-Type header for the response,
    /// which may be handled using <see cref="T:Microsoft.Net.Http.Headers.MediaTypeHeaderValue" />.
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="Microsoft.AspNetCore.Http.StatusCodes">HTTP status code</see>.
    /// </summary>
    public int? StatusCode { get; set; }

    /// <inheritdoc />
    public override Task ExecuteResultAsync(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var executor = context.HttpContext.RequestServices.GetRequiredService<IActionResultExecutor<ContentResult>>();
        return executor.ExecuteAsync(context, this);
    }
}
