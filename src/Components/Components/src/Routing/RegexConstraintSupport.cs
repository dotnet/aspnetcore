// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

internal static class RegexConstraintSupport
{
    // We should check the AppContext switch in the implementation, but it doesn't flow to the wasm runtime
    // during development, so we can't offer a better experience (build time message to enable the switch)
    // until the context switch flows to the runtime.
    // This value gets updated by the linker when the app is trimmed, so the code will always be removed from
    // webassembly unless the switch is enabled.
    public static bool IsEnabled =>
        AppContext.TryGetSwitch("Microsoft.AspNetCore.Components.Routing.RegexConstraintSupport", out var enabled) && enabled;
}
