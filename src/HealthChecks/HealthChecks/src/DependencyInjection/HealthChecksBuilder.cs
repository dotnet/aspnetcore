// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class HealthChecksBuilder : IHealthChecksBuilder
{
    public HealthChecksBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public IHealthChecksBuilder Add(HealthCheckRegistration registration)
    {
        ArgumentNullThrowHelper.ThrowIfNull(registration);

        Services.Configure<HealthCheckServiceOptions>(options =>
        {
            options.Registrations.Add(registration);
        });

        return this;
    }
}
