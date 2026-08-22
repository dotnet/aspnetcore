// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Virtualization;

internal sealed class VirtualizeAlignmentResult
{
    public ViewportFillDirection FillDirection { get; set; }

    public float SpacerSeparation { get; set; }

    public float ContainerSize { get; set; }

    public long RenderedWindowVersion { get; set; }
}
