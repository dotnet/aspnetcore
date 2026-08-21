// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Virtualization;

internal interface IVirtualizeJsCallbacks
{
    long RenderedWindowVersion { get; }
    void OnBeforeSpacerVisible(float spacerSize, float spacerSeparation, float containerSize, SpacerVisibilityReason reason, long renderedWindowVersion);
    void OnAfterSpacerVisible(float spacerSize, float spacerSeparation, float containerSize, SpacerVisibilityReason reason, long renderedWindowVersion);
    void OnAlignmentCompleted(VirtualizeAlignmentResult result);
}
