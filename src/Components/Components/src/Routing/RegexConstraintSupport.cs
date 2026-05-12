// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

internal static class RegexConstraintSupport
{
    public static bool IsEnabled =>
        AppContext.TryGetSwitch("Microsoft.AspNetCore.Components.Routing.RegexConstraintSupport", out var enabled) && enabled;
}
