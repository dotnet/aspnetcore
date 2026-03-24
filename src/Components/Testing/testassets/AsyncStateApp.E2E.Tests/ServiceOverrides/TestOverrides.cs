// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AsyncStateApp.Services;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncStateApp.E2E.Tests.ServiceOverrides;

// Service override that replaces IWeatherService with a test-controlled version.
// Uses TestLockProvider to gate data loading: the service blocks until the test
// releases the lock via POST /_test/lock/release?key={sessionId}:weather-data.
class TestOverrides
{
    public static void LockableWeather(IServiceCollection services)
    {
        services.AddScoped<IWeatherService, LockableWeatherService>();
    }
}
