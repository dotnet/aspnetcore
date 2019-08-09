// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class BlazorHostingApplicationBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseClientSideBlazorFiles(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, string clientAssemblyFilePath) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseClientSideBlazorFiles<TClientApp>(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
    public static partial class BlazorHostingEndpointRouteBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToClientSideBlazor(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string clientAssemblyFilePath, string filePath) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToClientSideBlazor(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string clientAssemblyFilePath, string pattern, string filePath) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToClientSideBlazor<TClientApp>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string filePath) { throw null; }
        public static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder MapFallbackToClientSideBlazor<TClientApp>(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, string pattern, string filePath) { throw null; }
    }
    public static partial class BlazorMonoDebugProxyAppBuilderExtensions
    {
        public static void UseBlazorDebugging(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { }
    }
}
