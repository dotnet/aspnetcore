// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    /// <summary>
    /// A builder used to collect instances of <see cref="IHealthCheck"/> and register them on an <see cref="IServiceCollection"/>.
    /// </summary>
    /// <remarks>
    /// This type wraps an <see cref="IServiceCollection"/> and provides a place for health check components to attach extension
    /// methods for registering themselves in the <see cref="IServiceCollection"/>.
    /// </remarks>
    public interface IHealthChecksBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> into which <see cref="IHealthCheck"/> instances should be registered.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
