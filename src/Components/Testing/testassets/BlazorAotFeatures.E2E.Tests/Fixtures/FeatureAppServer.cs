// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;

namespace BlazorAotFeatures.E2E.Tests.Fixtures;

/// <summary>
/// Starts the feature app under the harness.
/// </summary>
/// <remarks>
/// Every call produces the same <see cref="ServerStartOptions"/>, so the fixture hands back a single
/// shared instance for the whole collection.
/// </remarks>
internal static class FeatureAppServer
{
    public static Task<ServerInstance> StartAsync(ServerFixture<E2ETestAssembly> fixture)
        => fixture.StartServerAsync("BlazorServerAotSample", options =>
        {
            options.EnvironmentVariables["ASPNETCORE_DETAILEDERRORS"] = "true";

            // ILC takes minutes; the binary it produces is also slower to start than a JIT build.
            options.ReadinessTimeoutMs = 120_000;
        });
}
