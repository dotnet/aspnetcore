// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace System
{
    /// <summary>
    /// Helpful extension methods on IServiceProvider.
    /// </summary>
    internal static class LoggingServiceProviderExtensions
    {
        /// <summary>
        /// Retrieves an instance of ILogger given the type name of the caller.
        /// The caller's type name is used as the name of the ILogger created.
        /// This method returns null if the IServiceProvider is null or if it
        /// does not contain a registered ILoggerFactory.
        /// </summary>
        public static ILogger GetLogger<T>(this IServiceProvider services)
        {
            return services?.GetService<ILoggerFactory>()?.CreateLogger<T>();
        }
    }
}
