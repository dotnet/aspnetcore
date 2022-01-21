// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A <see cref="StatusCodeResult"/> that when
/// executed will produce a Unprocessable Entity (422) response.
/// </summary>
[DefaultStatusCode(DefaultStatusCode)]
public class UnprocessableEntityResult : StatusCodeResult
{
    private const int DefaultStatusCode = StatusCodes.Status422UnprocessableEntity;

    /// <summary>
    /// Creates a new <see cref="UnprocessableEntityResult"/> instance.
    /// </summary>
    public UnprocessableEntityResult()
        : base(DefaultStatusCode)
    {
    }
}
