// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// produce an HTTP response with the No Content (204) status code.
/// </summary>
public class NoContent : IResult, IEndpointMetadataProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoContent"/> class.
    /// </summary>
    internal NoContent()
    {
    }

    /// <summary>
    /// Gets the HTTP status code: <see cref="StatusCodes.Status204NoContent"/>
    /// </summary>
    public int StatusCode => StatusCodes.Status204NoContent;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.NoContentResult");

        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);

        httpContext.Response.StatusCode = StatusCode;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    static void IEndpointMetadataProvider.PopulateMetadata(EndpointMetadataContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.EndpointMetadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status204NoContent));
    }
}
