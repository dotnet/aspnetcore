// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Components.TestServer.RazorComponents;

internal static class NavigationCompletionTracker
{
    internal const string GuardSwitchName = "Components.TestServer.RazorComponents.UseNavigationCompletionGuard";

    private const string TrackedPathSuffix = "with-lazy-assembly";
    private static int _isNavigationTracked;
    private static int _isNavigationCompleted;

    public static bool TryGetGuardTask(string? path, out Task guardTask)
    {
        if (!IsGuardEnabledForPath(path))
        {
            guardTask = Task.CompletedTask;
            return false;
        }

        guardTask = TrackNavigationAsync();
        return true;
    }

    public static void AssertNavigationCompleted()
    {
        if (Volatile.Read(ref _isNavigationTracked) == 1 && Volatile.Read(ref _isNavigationCompleted) == 0)
        {
            throw new InvalidOperationException("Navigation finished before OnNavigateAsync work completed.");
        }

        Volatile.Write(ref _isNavigationTracked, 0);
    }

    private static bool IsGuardEnabledForPath(string? path)
    {
        if (!AppContext.TryGetSwitch(GuardSwitchName, out var isEnabled) || !isEnabled)
        {
            return false;
        }

        return path is not null && path.EndsWith(TrackedPathSuffix, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task TrackNavigationAsync()
    {
        Volatile.Write(ref _isNavigationTracked, 1);
        Volatile.Write(ref _isNavigationCompleted, 0);

        try
        {
            await Task.Yield();
            await Task.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _isNavigationCompleted, 1);
        }
    }
}
