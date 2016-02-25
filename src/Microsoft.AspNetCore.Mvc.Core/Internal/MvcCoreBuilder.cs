// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Allows fine grained configuration of essential MVC services.
    /// </summary>
    public class MvcCoreBuilder : IMvcCoreBuilder
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MvcCoreBuilder"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        public MvcCoreBuilder(IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            Services = services;
        }

        /// <inheritdoc />
        public IServiceCollection Services { get; }
    }
}