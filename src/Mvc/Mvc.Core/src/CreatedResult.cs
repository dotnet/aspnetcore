// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ActionResult"/> that returns a Created (201) response with a Location header.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class CreatedResult : ObjectResult
{
    private const int DefaultStatusCode = StatusCodes.Status201Created;

    private string? _location;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedResult"/> class
    /// </summary>
    public CreatedResult()
        : base(null)
    {
        StatusCode = DefaultStatusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="location">The location at which the content has been created.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public CreatedResult(string? location, object? value)
        : base(value)
    {
        if (location != null)
        {
            Location = location;
        }

        StatusCode = DefaultStatusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatedResult"/> class with the values
    /// provided.
    /// </summary>
    /// <param name="location">The location at which the content has been created.</param>
    /// <param name="value">The value to format in the entity body.</param>
    public CreatedResult(Uri? location, object? value)
        : base(value)
    {
        if (location != null)
        {
            if (location.IsAbsoluteUri)
            {
                Location = location.AbsoluteUri;
            }
            else
            {
                Location = location.GetComponents(UriComponents.SerializationInfoString, UriFormat.UriEscaped);
            }
        }

        StatusCode = DefaultStatusCode;
    }

    /// <summary>
    /// Gets or sets the location at which the content has been created.
    /// </summary>
    public string? Location
    {
        get => _location;
        set => _location = value;
    }

    /// <inheritdoc />
    public override void OnFormatting(ActionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        base.OnFormatting(context);

        context.HttpContext.Response.Headers.Location = Location;
    }
}
