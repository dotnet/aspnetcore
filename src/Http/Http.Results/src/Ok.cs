// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.HttpResults;

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with Ok (200) status code.
/// </summary>
public sealed class Ok : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Ok"/> class with the values.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    internal Ok()
    {
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => StatusCodes.Status200OK;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.OkObjectResult");

        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);
        httpContext.Response.StatusCode = StatusCode;

        return Task.CompletedTask;
    }
}
