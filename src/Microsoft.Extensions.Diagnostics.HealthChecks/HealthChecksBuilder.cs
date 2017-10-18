// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.HealthChecks
{
    internal class HealthChecksBuilder : IHealthChecksBuilder
    {
        public IServiceCollection Services { get; }

        public HealthChecksBuilder(IServiceCollection services)
        {
            Services = services;
        }
    }
}
