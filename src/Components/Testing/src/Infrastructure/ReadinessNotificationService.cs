// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

// Hosted service that signals the test harness when the app is fully started.
// Listens to IHostApplicationLifetime.ApplicationStarted; when it fires, POSTs
// to the callback URL provided via the E2E_READY_URL environment variable.
// This replaces health-check polling with a one-shot push signal.
internal class ReadinessNotificationService : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;

    public ReadinessNotificationService(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var readyUrl = Environment.GetEnvironmentVariable("E2E_READY_URL");
        if (string.IsNullOrEmpty(readyUrl))
        {
            return Task.CompletedTask;
        }

        _lifetime.ApplicationStarted.Register(() =>
        {
            // Fire-and-forget: notify the test fixture that this app is listening.
            // Errors are intentionally swallowed — the test fixture has a timeout fallback.
            _ = NotifyReadyAsync(readyUrl);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task NotifyReadyAsync(string url)
    {
        const int maxAttempts = 5;
        var delay = TimeSpan.FromMilliseconds(200);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.PostAsync(url, content: null).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Retry on any error
            }

            if (attempt < maxAttempts)
            {
                await Task.Delay(delay).ConfigureAwait(false);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2);
            }
        }
    }
}
