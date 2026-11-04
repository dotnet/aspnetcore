// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// An <see cref="ObjectResult"/> that when executed will produce a Not Found (404) response.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class NotFoundObjectResult : ObjectResult
{
    private const int DefaultStatusCode = StatusCodes.Status404NotFound;

    /// <summary>
    /// Creates a new <see cref="NotFoundObjectResult"/> instance.
    /// </summary>
    /// <param name="value">The value to format in the entity body.</param>
    public NotFoundObjectResult([ActionResultObjectValue] object? value)
        : base(value)
    {
        StatusCode = DefaultStatusCode;
    }
}
