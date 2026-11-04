// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A <see cref="StatusCodeResult"/> that when executed will produce a Conflict (409) response.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class ConflictResult : StatusCodeResult
{
    private const int DefaultStatusCode = StatusCodes.Status409Conflict;

    /// <summary>
    /// Creates a new <see cref="ConflictResult"/> instance.
    /// </summary>
    public ConflictResult()
        : base(DefaultStatusCode)
    {
    }
}
