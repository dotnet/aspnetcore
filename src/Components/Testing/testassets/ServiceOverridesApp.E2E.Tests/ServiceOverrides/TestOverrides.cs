// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using ServiceOverridesApp.Services;

namespace ServiceOverridesApp.E2E.Tests.ServiceOverrides;

// Static method service override (WAF-like pattern).
// Registered via options.ConfigureServices<TestOverrides>(nameof(TestOverrides.FakeWeather))
// which sends the class + method name as env vars to the app process.
// The source generator detects this callsite and emits a direct delegate reference,
// avoiding reflection at runtime.
class TestOverrides
{
    public static void FakeWeather(IServiceCollection services)
    {
        // Last-registration-wins: this replaces the app's DefaultWeatherService
        services.AddSingleton<IWeatherService, FakeWeatherService>();
    }
}
