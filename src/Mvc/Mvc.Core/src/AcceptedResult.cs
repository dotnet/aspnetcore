// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ActionResult"/> that returns an Accepted (202) response with a Location header.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class AcceptedResult : ObjectResult
{
    private const int DefaultStatusCode = StatusCodes.Status202Accepted;

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedResult"/> class with the values
    /// provided.
    /// </summary>
    public AcceptedResult()
        : base(value: null)
    {
        StatusCode = DefaultStatusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="location">The location at which the status of requested content can be monitored.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public AcceptedResult(string? location, [ActionResultObjectValue] object? value)
        : base(value)
    {
        Location = location;
        StatusCode = DefaultStatusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AcceptedResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="locationUri">The location at which the status of requested content can be monitored.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public AcceptedResult(Uri locationUri, [ActionResultObjectValue] object? value)
        : base(value)
    {
        ArgumentNullException.ThrowIfNull(locationUri);

        if (locationUri.IsAbsoluteUri)
        {
            Location = locationUri.AbsoluteUri;
        }
        else
        {
            Location = locationUri.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
        }

        StatusCode = DefaultStatusCode;
    }

    /// <summary>
    /// Gets or sets the location at which the status of the requested content can be monitored.
    /// </summary>
    public string? Location { get; set; }

    /// <inheritdoc />
    public override void OnFormatting(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        base.OnFormatting(context);

        if (!string.IsNullOrEmpty(Location))
        {
            context.HttpContext.Response.Headers.Location = Location;
        }
    }
}
