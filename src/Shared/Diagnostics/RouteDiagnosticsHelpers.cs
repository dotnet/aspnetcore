// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Shared;

internal static class RouteDiagnosticsHelpers
{
    public static string ResolveHttpRoute(string route)
    {
        // A route that matches the root of the website could be an empty string. This is problematic.
        // 1. It is potentially confusing, "What does empty string mean?"
        // 2. Some telemetry tools have problems with empty string values, e.g. https://github.com/dotnet/aspnetcore/pull/62432
        //
        // The fix is to resolve empty string route to "/" in metrics.
        return string.IsNullOrEmpty(route) ? "/" : route;
    }
}
