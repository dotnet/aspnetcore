// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

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
