// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using System.Threading.Tasks;

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
    public AcceptedHttpResult(string? location, object? value)
    {
        Value = value;
        Location = location;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="locationUri">The location at which the status of requested content can be monitored.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public AcceptedHttpResult(Uri locationUri, object? value)
    {
        Value = value;

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

    /// <inheritdoc/>
    public object? Value { get; }

    /// <inheritdoc/>
    public int StatusCode => StatusCodes.Status202Accepted;

    /// <inheritdoc/>
    public string? Location { get; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        return HttpResultsWriter.WriteResultAsJsonAsync(
                httpContext,
                Value,
                StatusCode,
                configureResponseHeader: (context) =>
                {
                    if (!string.IsNullOrEmpty(Location))
                    {
                        context.Response.Headers.Location = Location;
                    }
                });
    }
}
