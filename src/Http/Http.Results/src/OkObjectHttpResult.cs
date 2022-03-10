// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal sealed class OkObjectHttpResult : ObjectHttpResult
{
    public OkObjectHttpResult(object? value)
        : base(value, StatusCodes.Status200OK)
    {
    }
}
