// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting
{
    public static class WebAssemblyHostBuilderExtensions
    {
        /// <summary>
        /// Adds a delegate for configuring the provided <see cref="ILoggingBuilder"/>. This may be called multiple times.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="WebAssemblyHostBuilder" /> to configure.</param>
        /// <param name="configureLogging">The delegate that configures the <see cref="ILoggingBuilder"/>.</param>
        /// <returns>The same instance of the <see cref="WebAssemblyHostBuilder"/> for chaining.</returns>
        public static WebAssemblyHostBuilder ConfigureLogging(this WebAssemblyHostBuilder hostBuilder, Action<ILoggingBuilder> configureLogging)
        {
            return hostBuilder.ConfigureServices((collection) => collection.AddLogging(builder => configureLogging(builder)));
        }
    }
}
