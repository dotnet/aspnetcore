// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents an <see cref="ActionResult"/> that when executed will
/// produce an HTTP response with the given response status code.
/// </summary>
public partial class StatusCodeResult : ActionResult, IClientErrorActionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCodeResult"/> class
    /// with the given <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    public StatusCodeResult([ActionResultStatusCode] int statusCode)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    int? IStatusCodeActionResult.StatusCode => StatusCode;

    /// <inheritdoc />
    public override void ExecuteResult(ActionContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var httpContext = context.HttpContext;
        var factory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = factory.CreateLogger<StatusCodeResult>();
        Log.HttpStatusCodeResultExecuting(logger, StatusCode);

        httpContext.Response.StatusCode = StatusCode;
    }

    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Executing StatusCodeResult, setting HTTP status code {StatusCode}", EventName = "HttpStatusCodeResultExecuting")]
        public static partial void HttpStatusCodeResultExecuting(ILogger logger, int statusCode);
    }
}
