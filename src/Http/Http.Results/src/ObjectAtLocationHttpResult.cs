// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with a Location header to the supplied URL.
/// </summary>
public abstract class ObjectAtLocationHttpResult : ObjectHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectAtLocationHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="location">The location at which the status of requested content can be monitored.</param>
    /// <param name="value">The value to format in the entity body.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    internal ObjectAtLocationHttpResult(
        string? location,
        object? value,
        int? statusCode)
        : base(value, statusCode)
    {
        Location = location;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="locationUri">The location at which the status of requested content can be monitored.</param>
    /// <param name="value">The value to format in the entity body.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    internal ObjectAtLocationHttpResult(
        Uri locationUri,
        object? value,
        int? statusCode)
        : base(value, statusCode)
    {
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
    /// Gets the location at which the status of the requested content can be monitored.
    /// </summary>
    public string? Location { get; }

    /// <inheritdoc />
    protected internal override void ConfigureResponseHeaders(HttpContext context)
    {
        if (!string.IsNullOrEmpty(Location))
        {
            context.Response.Headers.Location = Location;
        }
    }
}
