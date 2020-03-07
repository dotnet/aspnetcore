// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

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
            // Behave like async void, because we don't yet support async-main properly on WebAssembly.
            // However, don't actually make this method async, because we rely on startup being synchronous
            // for things like attaching navigation event handlers.
            host.StartAsync().ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    Console.WriteLine(task.Exception);
                }
            });
        }
    }
}
