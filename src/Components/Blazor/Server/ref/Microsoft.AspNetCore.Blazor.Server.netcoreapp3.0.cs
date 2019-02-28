// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class BlazorApplicationBuilderExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseBlazor(this Microsoft.AspNetCore.Builder.IApplicationBuilder app, Microsoft.AspNetCore.Builder.BlazorOptions options) { throw null; }
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseBlazor<TProgram>(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
    public static partial class BlazorMonoDebugProxyAppBuilderExtensions
    {
        public static void UseBlazorDebugging(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { }
    }
    public partial class BlazorOptions
    {
        public BlazorOptions() { }
        public string ClientAssemblyPath { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
