// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
