// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ObjectResult"/> that when executed performs content negotiation, formats the entity body, and
/// will produce a <see cref="StatusCodes.Status200OK"/> response if negotiation and formatting succeed.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class OkObjectResult : ObjectResult
{
    private const int DefaultStatusCode = StatusCodes.Status200OK;

    /// <summary>
    /// Initializes a new instance of the <see cref="OkObjectResult"/> class.
    /// </summary>
    /// <param name="value">The content to format into the entity body.</param>
    public OkObjectResult(object? value)
        : base(value)
    {
        StatusCode = DefaultStatusCode;
    }
}
