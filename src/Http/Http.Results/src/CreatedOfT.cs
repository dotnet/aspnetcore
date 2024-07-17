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
/// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
public sealed class Created<TValue> : IResult, IEndpointMetadataProvider, IStatusCodeHttpResult, IValueHttpResult, IValueHttpResult<TValue>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Created"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="location">The location at which the content has been created.</param>
    /// <param name="value">The value to format in the entity body.</param>
    internal Created(string? location, TValue? value)
    {
        Value = value;
        Location = location;
        HttpResultsHelper.ApplyProblemDetailsDefaultsIfNeeded(Value, StatusCode);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Created"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="locationUri">The location at which the content has been created.</param>
    /// <param name="value">The value to format in the entity body.</param>
    internal Created(Uri? locationUri, TValue? value)
    {
        Value = value;
        HttpResultsHelper.ApplyProblemDetailsDefaultsIfNeeded(Value, StatusCode);

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
    /// Gets the object result.
    /// </summary>
    public TValue? Value { get; }

    object? IValueHttpResult.Value => Value;

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

        if (!string.IsNullOrEmpty(Location))
        {
            httpContext.Response.Headers.Location = Location;
        }

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.CreatedResult");

        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);
        httpContext.Response.StatusCode = StatusCode;

        return HttpResultsHelper.WriteResultAsJsonAsync(
                httpContext,
                logger,
                Value);
    }

    /// <inheritdoc/>
    static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(builder);

        builder.Metadata.Add(ProducesResponseTypeMetadata.CreateUnvalidated(typeof(TValue), StatusCodes.Status201Created, ContentTypeConstants.ApplicationJsonContentTypes));
    }
}
