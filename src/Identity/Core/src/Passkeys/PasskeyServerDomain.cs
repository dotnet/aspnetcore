// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Identity;

internal static class PasskeyServerDomain
{
    public static string Resolve(IdentityPasskeyOptions options, HttpContext httpContext)
        => options.ServerDomain ?? httpContext.Request.Host.Host;
}
