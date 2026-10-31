// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

internal enum ActionType
{
    None,
    Rewrite,
    Redirect,
    CustomResponse,
    AbortRequest
}
