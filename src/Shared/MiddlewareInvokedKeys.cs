// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal static class MiddlewareInvokedKeys
{
    public const string Antiforgery = "__AntiforgeryMiddlewareWithEndpointInvoked";
    public const string CsrfProtection = "__CsrfProtectionMiddlewareWithEndpointInvoked";

    /// <summary>
    /// Key for a Func&lt;RequestDelegate, RequestDelegate&gt; stored in IApplicationBuilder.Properties by WebApplicationBuilder.
    /// It composes the framework's implicit post-routing middleware pipeline.
    /// </summary>
    public const string PostRoutingPipeline = "__PostRoutingPipeline";

    public static readonly object Sentinel = new();
}
