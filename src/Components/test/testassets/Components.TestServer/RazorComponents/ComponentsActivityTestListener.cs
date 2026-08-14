// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Components.TestServer.RazorComponents;

internal static class ComponentsActivityTestListener
{
    private static int _enabled;
    private static readonly ActivityListener _listener = new()
    {
        ShouldListenTo = source => source.Name is "Microsoft.AspNetCore.Components" or "Microsoft.AspNetCore.Components.Server.Circuits",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
            Volatile.Read(ref _enabled) == 1 ? ActivitySamplingResult.AllData : ActivitySamplingResult.None,
    };

    static ComponentsActivityTestListener()
    {
        ActivitySource.AddActivityListener(_listener);
    }

    public static void Enable()
    {
        Volatile.Write(ref _enabled, 1);
    }

    public static void Disable()
    {
        Volatile.Write(ref _enabled, 0);
    }
}
