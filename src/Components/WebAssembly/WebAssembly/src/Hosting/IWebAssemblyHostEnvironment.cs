// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    /// <summary>
    /// Provides information about the hosting environment an application is running in.
    /// </summary>
    public interface IWebAssemblyHostEnvironment
    {
        /// <summary>
        /// Gets the name of the environment. This is configured to use the environment of the application hosting the Blazor WebAssembly application.
        /// Configured to "Production" when not specified by the host.
        /// </summary>
        string Environment { get; }

        /// <summary>
        /// Gets the base address for the application. This is typically derived from the <c>&gt;base href&lt;</c> value in the host page.
        /// </summary>
        string BaseAddress { get; }
    }
}
