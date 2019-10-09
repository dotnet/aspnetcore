// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection.Internal
{
    /// <summary>
    /// Default implementation of <see cref="IDataProtectionBuilder"/>.
    /// </summary>
    internal class DataProtectionBuilder : IDataProtectionBuilder
    {
        /// <summary>
        /// Creates a new configuration object linked to a <see cref="IServiceCollection"/>.
        /// </summary>
        public DataProtectionBuilder(IServiceCollection services)
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
