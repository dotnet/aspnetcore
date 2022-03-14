// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// An <see cref="IResult"/> that on execution will write an object to the response
/// with status code Created (201) and Location header.
/// </summary>
public sealed class CreatedHttpResult : IResult, IObjectHttpResult, IStatusCodeHttpResult, IAtLocationHttpResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="location">The location at which the content has been created.</param>
    /// <param name="value">The value to format in the entity body.</param>
    internal CreatedHttpResult(string location, object? value)
    {
        Value = value;
        Location = location;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedHttpResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="locationUri">The location at which the content has been created.</param>
    /// <param name="value">The value to format in the entity body.</param>
    internal CreatedHttpResult(Uri locationUri, object? value)
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
    public int? StatusCode => StatusCodes.Status201Created;

    /// <inheritdoc/>
    public string? Location { get; }

    /// <inheritdoc/>
    public Task ExecuteAsync(HttpContext httpContext)
        => HttpResultsWriter.WriteResultAsJson(
            httpContext,
            objectHttpResult: this,
            configureResponseHeader: ConfigureResponseHeaders);

    private void ConfigureResponseHeaders(HttpContext context)
    {
        if (!string.IsNullOrEmpty(Location))
        {
            context.Response.Headers.Location = Location;
        }
    }
}
