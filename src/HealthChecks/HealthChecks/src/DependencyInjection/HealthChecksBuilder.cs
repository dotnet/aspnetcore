// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class HealthChecksBuilder : IHealthChecksBuilder
    {
        public HealthChecksBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
        
        public IHealthChecksBuilder Add(HealthCheckRegistration registration)
        {
            if (registration == null)
            {
                throw new ArgumentNullException(nameof(registration));
            }

            Services.Configure<HealthCheckServiceOptions>(options =>
            {
                options.Registrations.Add(registration);
            });

            return this;
        }
    }
}
