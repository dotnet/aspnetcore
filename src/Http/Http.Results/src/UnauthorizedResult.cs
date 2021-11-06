// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Result;

internal sealed class UnauthorizedResult : StatusCodeResult
{
    public UnauthorizedResult() : base(StatusCodes.Status401Unauthorized)
    {
    }
}
