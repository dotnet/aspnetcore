// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with status code Created (201) and Location header.
/// </summary>
public sealed class Created : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Created"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="location">The location at which the content has been created.</param>
    internal Created(string? location)
    {
        Location = location;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Created"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="locationUri">The location at which the content has been created.</param>
    internal Created(Uri? locationUri)
    {
        if (locationUri != null)
        {
            if (locationUri.IsAbsoluteUri)
            {
                Location = locationUri.AbsoluteUri;
            }
            else
            {
                Location = locationUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            }
        }
    }

    /// <summary>
    /// Gets the HTTP status code: <see cref="StatusCodes.Status201Created"/>
    /// </summary>
    public int StatusCode => StatusCodes.Status201Created;

    int? IStatusCodeHttpResult.StatusCode => StatusCode;

    /// <inheritdoc/>
    public string? Location { get; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.CreatedResult");

        if (!string.IsNullOrEmpty(Location))
        {
            httpContext.Response.Headers.Location = Location;
        }

        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);
        httpContext.Response.StatusCode = StatusCode;

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(new ProducesResponseTypeMetadata(StatusCodes.Status201Created, typeof(void)));
    }
}
