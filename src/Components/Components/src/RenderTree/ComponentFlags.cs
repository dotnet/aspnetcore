// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// Describes flags associated with a <see cref="RenderTreeFrame"/> whose <see cref="RenderTreeFrame.FrameType"/>
    /// equals <see cref="RenderTreeFrameType.Component"/>.
    /// </summary>
    [Flags]
    public enum ComponentFlags: short
    {
        LooseKey = 1,
    }
}
