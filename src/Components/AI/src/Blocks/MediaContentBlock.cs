// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.AI;

internal sealed class MediaContentBlock : ContentBlock
{
    public RenderFragment Content { get; set; } = default!;

    public string? Name { get; set; }
}
