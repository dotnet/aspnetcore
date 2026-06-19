// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal static class MiddlewareInvokedKeys
{
    public const string Antiforgery = "__AntiforgeryMiddlewareWithEndpointInvoked";
    public const string CsrfProtection = "__CsrfProtectionMiddlewareWithEndpointInvoked";

    public static readonly object Sentinel = new();
}
