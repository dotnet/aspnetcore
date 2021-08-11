// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Options for controlling the behavior of <see cref="RequestDelegate" /> when created using <see cref="RequestDelegateFactory" />.
    /// </summary>
    public sealed class RequestDelegateFactoryOptions
    {
        /// <summary>
        /// The <see cref="IServiceProvider"/> instance used to detect if handler parameters are services.
        /// </summary>
        public IServiceProvider? ServiceProvider { get; init; }

        /// <summary>
        /// The list of route parameter names that are specified for this handler.
        /// </summary>
        public IEnumerable<string>? RouteParameterNames { get; init; }

        /// <summary>
        /// Check if the reques has a body
        /// </summary>
#pragma warning disable RS0016 // Add public types and members to the declared API
        public bool HasBodyParameter { get; set; }
#pragma warning restore RS0016 // Add public types and members to the declared API
    }
}
