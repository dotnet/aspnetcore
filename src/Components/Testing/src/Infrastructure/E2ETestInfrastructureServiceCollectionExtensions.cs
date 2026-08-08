// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Registers the E2E test infrastructure into an application's service collection.
/// </summary>
internal static class E2ETestInfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services the test harness needs in the application process: readiness notification,
    /// parent-process watching, and the session/lock plumbing for deterministic async state.
    /// </summary>
    /// <remarks>
    /// This is the registration path for applications the harness reaches through
    /// <c>ASPNETCORE_HOSTINGSTARTUPASSEMBLIES</c>, via <see cref="TestReadinessHostingStartup"/>. A
    /// Native AOT application cannot load an assembly by name at startup, so it gets an equivalent
    /// harness written into its own compilation by the testing source generator instead, and never
    /// references this assembly.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddE2ETestInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHostedService<ReadinessNotificationService>();
        services.AddHostedService<ParentProcessWatcher>();

        services.AddScoped<TestSessionContext>();
        services.AddSingleton<TestLockProvider>();
        services.AddTransient<IStartupFilter, TestInfrastructureStartupFilter>();

        return services;
    }
}
