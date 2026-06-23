// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

internal static class MiddlewareInvokedKeys
{
    public const string Antiforgery = "__AntiforgeryMiddlewareWithEndpointInvoked";
    public const string CsrfProtection = "__CsrfProtectionMiddlewareWithEndpointInvoked";

    // Key for a Func<RequestDelegate, RequestDelegate> stored in IApplicationBuilder.Properties by WebApplicationBuilder.
    // It composes the framework's implicit post-routing middleware. When the app calls UseRouting() explicitly,
    // UseRouting() runs this block immediately after the endpoint is matched so the implicit middleware observe the matched endpoint, 
    // matching the behavior of the framework's implicit UseRouting(). See #67174.
    public const string PostRoutingPipeline = "__PostRoutingPipeline";

    public static readonly object Sentinel = new();
}
