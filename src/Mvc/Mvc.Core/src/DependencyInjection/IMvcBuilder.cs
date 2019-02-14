// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// An interface for configuring MVC services.
    /// </summary>
    public interface IMvcBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> where MVC services are configured.
        /// </summary>
        IServiceCollection Services { get; }

        /// <summary>
        /// Gets the <see cref="ApplicationPartManager"/> where <see cref="ApplicationPart"/>s
        /// are configured.
        /// </summary>
        ApplicationPartManager PartManager { get; }
    }
}
