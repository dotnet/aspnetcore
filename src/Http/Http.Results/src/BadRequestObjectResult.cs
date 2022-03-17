// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Result;

internal sealed class BadRequestObjectResult : ObjectResult
{
    public BadRequestObjectResult(object? error)
        : base(error, StatusCodes.Status400BadRequest)
    {
    }
}
