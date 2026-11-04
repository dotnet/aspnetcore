// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents an <see cref="StatusCodeResult"/> that when
/// executed will produce a Not Found (404) response.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class NotFoundResult : StatusCodeResult
{
    private const int DefaultStatusCode = StatusCodes.Status404NotFound;

    /// <summary>
    /// Creates a new <see cref="NotFoundResult"/> instance.
    /// </summary>
    public NotFoundResult() : base(DefaultStatusCode)
    {
    }
}
