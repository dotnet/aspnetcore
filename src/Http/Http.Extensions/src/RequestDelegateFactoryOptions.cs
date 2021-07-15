// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Options for controlling the behavior 
    /// </summary>
    public class RequestDelegateFactoryOptions
    {
        /// <summary>
        /// The <see cref="IServiceProvider"/> instance used to detect if handler parameters are services.
        /// </summary>
        public IServiceProvider? ServiceProvider { get; init; }

        /// <summary>
        /// The list of route parameter names that are specified for this handler.
        /// </summary>
        public IEnumerable<string>? RouteParameterNames { get; init; }
    }
}
