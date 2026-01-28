// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Company.WorkerClassLibrary1;

/// <summary>
/// Extension methods for registering WorkerClient services.
/// </summary>
public static class WorkerClientServiceExtensions
{
    /// <summary>
    /// Adds the WorkerClient service to the service collection.
    /// </summary>
    public static IServiceCollection AddWorkerClient(this IServiceCollection services)
    {
        return services.AddScoped<IWorkerClient, WorkerClient>();
    }
}
