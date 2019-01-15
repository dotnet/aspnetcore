// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.Components.Server.Builder
{
    /// <summary>
    /// Specifies options to configure <see cref="RazorComponentsApplicationBuilderExtensions.UseRazorComponents{TStartup}(IApplicationBuilder)"/>
    /// </summary>
    public class RazorComponentsOptions
    {
        /// <summary>
        /// Gets or sets a flag to indicate whether to attach middleware for
        /// communicating with interactive components via SignalR. Defaults
        /// to true.
        ///
        /// If the value is set to false, the application must manually add
        /// SignalR middleware with <see cref="BlazorHub"/>.
        /// </summary>
        public bool UseSignalRWithBlazorHub { get; set; } = true;
    }
}
