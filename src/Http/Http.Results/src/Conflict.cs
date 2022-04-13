// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with Conflict (409) status code.
/// </summary>
public sealed class Conflict : IResult, IEndpointMetadataProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Conflict"/> class with the values
    /// provided.
    /// </summary>
    internal Conflict()
    {
    }

    /// <summary>
    /// Gets the HTTP status code: <see cref="StatusCodes.Status409Conflict"/>
    /// </summary>
    public int StatusCode => StatusCodes.Status409Conflict;

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.ConflictObjectResult");

        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);
        httpContext.Response.StatusCode = StatusCode;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    static void IEndpointMetadataProvider.PopulateMetadata(EndpointMetadataContext context)
    {
        context.EndpointMetadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status409Conflict));
    }
}
