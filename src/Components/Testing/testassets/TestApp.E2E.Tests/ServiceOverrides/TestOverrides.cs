// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using TestApp.Services;

namespace TestApp.E2E.Tests.ServiceOverrides;

// Static method service overrides (WAF-like pattern).
// Registered via options.ConfigureServices<TestOverrides>(nameof(TestOverrides.XxxMethod))
class TestOverrides
{
    // Replaces IWeatherService with a deterministic fake for service override tests
    public static void FakeWeather(IServiceCollection services)
    {
        services.AddSingleton<IWeatherService, FakeWeatherService>();
    }

    // Replaces IWeatherService with a test-controlled lockable version for async state tests
    public static void LockableWeather(IServiceCollection services)
    {
        services.AddScoped<IWeatherService, LockableWeatherService>();
    }
}
