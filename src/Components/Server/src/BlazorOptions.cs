// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides configuration options to the <see cref="BlazorApplicationBuilderExtensions.UseBlazor(IApplicationBuilder, BlazorOptions)"/>
    /// middleware.
    /// </summary>
    public class BlazorOptions
    {
        /// <summary>
        /// Full path to the client assembly.
        /// </summary>
        public string ClientAssemblyPath { get; set; }
    }
}
