// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    /// <summary>
    /// Extension methods for <see cref="IWebAssemblyHost"/>.
    /// </summary>
    public static class WebAssemblyHostExtensions
    {
        /// <summary>
        /// Runs the application.
        /// </summary>
        /// <param name="host">The <see cref="IWebAssemblyHost"/> to run.</param>
        /// <remarks>
        /// Currently, Blazor applications running in the browser don't have a lifecycle - the application does not
        /// get a chance to gracefully shut down. For now, <see cref="Run(IWebAssemblyHost)"/> simply starts the host
        /// and allows execution to continue.
        /// </remarks>
        public static void Run(this IWebAssemblyHost host)
        {
            host.StartAsync().GetAwaiter().GetResult();
        }
    }
}