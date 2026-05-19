// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Represents an <see cref="UnauthorizedResult"/> that when
/// executed will produce an Unauthorized (401) response.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class UnauthorizedResult : StatusCodeResult
{
    private const int DefaultStatusCode = StatusCodes.Status401Unauthorized;

    /// <summary>
    /// Creates a new <see cref="UnauthorizedResult"/> instance.
    /// </summary>
    public UnauthorizedResult() : base(DefaultStatusCode)
    {
    }
}
