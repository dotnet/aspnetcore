// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with status code Accepted (202) and Location header.
/// Targets a registered route.
/// </summary>
public sealed class AcceptedHttpResult : IResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="location">The location at which the status of requested content can be monitored.</param>
    /// <param name="value">The value to format in the entity body.</param>
    internal AcceptedHttpResult(string? location, object? value)
    {
        Value = value;
        Location = location;
        HttpResultsHelper.ApplyProblemDetailsDefaultsIfNeeded(Value, StatusCode);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="locationUri">The location at which the status of requested content can be monitored.</param>
    /// <param name="value">The value to format in the entity body.</param>
    internal AcceptedHttpResult(Uri locationUri, object? value)
    {
        Value = value;
        HttpResultsHelper.ApplyProblemDetailsDefaultsIfNeeded(Value, StatusCode);

        if (locationUri == null)
        {
            throw new ArgumentNullException(nameof(locationUri));
        }

        if (locationUri.IsAbsoluteUri)
        {
            Location = locationUri.AbsoluteUri;
        }
        else
        {
            Location = locationUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
        }
    }

    /// <summary>
    /// Gets the object result.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode => StatusCodes.Status202Accepted;

    /// <summary>
    /// Gets the location at which the status of the requested content can be monitored.
    /// </summary>
    public string? Location { get; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        if (!string.IsNullOrEmpty(Location))
        {
            httpContext.Response.Headers.Location = Location;
        }

        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.AcceptedResult");
        return HttpResultsHelper.WriteResultAsJsonAsync(
                httpContext,
                logger,
                Value,
                StatusCode);
    }
}
