// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client
{
    /// <summary>
    /// Extension methods for <see cref="IHubConnectionBuilder"/>.
    /// </summary>
    public static class HubConnectionBuilderExtensions
    {
        /// <summary>
        /// Adds a delegate for configuring the provided <see cref="ILoggingBuilder"/>. This may be called multiple times.
        /// </summary>
        /// <param name="hubConnectionBuilder">The <see cref="IHubConnectionBuilder" /> to configure.</param>
        /// <param name="configureLogging">The delegate that configures the <see cref="ILoggingBuilder"/>.</param>
        /// <returns>The same instance of the <see cref="IHubConnectionBuilder"/> for chaining.</returns>
        public static IHubConnectionBuilder ConfigureLogging(this IHubConnectionBuilder hubConnectionBuilder, Action<ILoggingBuilder> configureLogging)
        {
            hubConnectionBuilder.Services.AddLogging(configureLogging);
            return hubConnectionBuilder;
        }
    }
}