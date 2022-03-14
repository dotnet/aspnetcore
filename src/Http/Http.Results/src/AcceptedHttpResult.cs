// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using System.Threading.Tasks;

/// <summary>
/// 
/// </summary>
public sealed class AcceptedHttpResult : IResult, IObjectHttpResult, IStatusCodeHttpResult, IAtLocationHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    internal AcceptedHttpResult()
    {
    }

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
    /// Gets or sets the object result.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int? StatusCode => StatusCodes.Status202Accepted;

    /// <summary>
    /// Gets the location at which the status of the requested content can be monitored.
    /// </summary>
    public string? Location { get; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(httpContext, Value, statusCode: StatusCode, responseHeader: ConfigureResponseHeaders);

    private void ConfigureResponseHeaders(HttpContext context)
    {
        if (!string.IsNullOrEmpty(Location))
        {
            context.Response.Headers.Location = Location;
        }
    }
}
