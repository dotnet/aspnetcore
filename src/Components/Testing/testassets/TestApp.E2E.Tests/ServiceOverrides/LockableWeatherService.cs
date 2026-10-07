// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using TestApp.Services;

namespace TestApp.E2E.Tests.ServiceOverrides;

// Weather service that awaits a test lock before returning deterministic data.
// Lock key convention: "{sessionId}:weather-data" where sessionId comes from
// the TestSessionContext (populated by the test-session-id cookie).
class LockableWeatherService : IWeatherService
{
    private readonly TestLockProvider _lockProvider;
    private readonly TestSessionContext _session;

    public LockableWeatherService(TestLockProvider lockProvider, TestSessionContext session)
    {
        _lockProvider = lockProvider;
        _session = session;
    }

    public async Task<WeatherForecast[]> GetForecastsAsync()
    {
        // Wait for the test to release the lock (or proceed immediately if no session)
        if (_session.Id is not null)
        {
            var lockKey = $"{_session.Id}:weather-data";
            await _lockProvider.WaitOn(lockKey);
        }

        return
        [
            new()
            {
                Date = new DateOnly(2025, 7, 1),
                TemperatureC = 25,
                Summary = "TestSunny"
            },
            new()
            {
                Date = new DateOnly(2025, 7, 2),
                TemperatureC = 18,
                Summary = "TestCloudy"
            }
        ];
    }
}
