// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

// Monitors the parent test process PID. When it exits (or is killed),
// this service shuts down the app to prevent orphaned server processes.
// The parent PID is communicated via the TEST_PARENT_PID env var.
internal class ParentProcessWatcher : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly int _parentPid;

    public ParentProcessWatcher(IHostApplicationLifetime lifetime)
    {
        _lifetime = lifetime;
        var pidStr = Environment.GetEnvironmentVariable("TEST_PARENT_PID");
        _parentPid = int.TryParse(pidStr, out var pid) ? pid : -1;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_parentPid <= 0)
        {
            return;
        }

        try
        {
            var parent = Process.GetProcessById(_parentPid);

            while (!stoppingToken.IsCancellationRequested)
            {
                if (parent.HasExited)
                {
                    _lifetime.StopApplication();
                    return;
                }

                await Task.Delay(1000, stoppingToken).ConfigureAwait(false);
            }
        }
        catch (ArgumentException)
        {
            // Parent process already gone
            _lifetime.StopApplication();
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
    }
}
