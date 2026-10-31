// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A <see cref="StatusCodeResult"/> that when executed will produce a 204 No Content response.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class NoContentResult : StatusCodeResult
{
    private const int DefaultStatusCode = StatusCodes.Status204NoContent;

    /// <summary>
    /// Initializes a new <see cref="NoContentResult"/> instance.
    /// </summary>
    public NoContentResult()
        : base(DefaultStatusCode)
    {
    }
}
