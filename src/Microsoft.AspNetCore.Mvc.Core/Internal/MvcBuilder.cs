// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Allows fine grained configuration of MVC services.
    /// </summary>
    public class MvcBuilder : IMvcBuilder
    {
        /// <summary>
        /// Initializes a new <see cref="MvcBuilder"/> instance.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
        public MvcBuilder(IServiceCollection services)
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