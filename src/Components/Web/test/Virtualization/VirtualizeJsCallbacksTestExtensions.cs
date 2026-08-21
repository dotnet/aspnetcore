// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace Microsoft.AspNetCore.Components.Virtualization;

internal static class VirtualizeJsCallbacksTestExtensions
{
    public static void OnBeforeSpacerVisible(
        this IVirtualizeJsCallbacks callbacks,
        float spacerSize,
        float spacerSeparation,
        float containerSize,
        SpacerVisibilityReason reason)
        => callbacks.OnBeforeSpacerVisible(
            spacerSize,
            spacerSeparation,
            containerSize,
            reason,
            callbacks.RenderedWindowVersion);

    public static void OnAfterSpacerVisible(
        this IVirtualizeJsCallbacks callbacks,
        float spacerSize,
        float spacerSeparation,
        float containerSize,
        SpacerVisibilityReason reason)
        => callbacks.OnAfterSpacerVisible(
            spacerSize,
            spacerSeparation,
            containerSize,
            reason,
            callbacks.RenderedWindowVersion);
}
