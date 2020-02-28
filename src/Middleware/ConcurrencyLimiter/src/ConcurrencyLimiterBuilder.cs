// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.ConcurrencyLimiter
{
    public class ConcurrencyLimiterBuilder
    {
        public ConcurrencyLimiterBuilder(IServiceCollection services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> services are attached to.
        /// </summary>
        /// <value>
        /// The <see cref="IServiceCollection"/> services are attached to.
        /// </value>
        public IServiceCollection Services { get; }
    }
}
