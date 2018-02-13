// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;
using System;

namespace Microsoft.AspNetCore.Blazor.Rendering
{
    internal readonly struct RenderQueueEntry
    {
        public readonly int ComponentId;
        public readonly Action<RenderTreeBuilder> RenderAction;

        public RenderQueueEntry(int componentId, Action<RenderTreeBuilder> renderAction)
        {
            ComponentId = componentId;
            RenderAction = renderAction ?? throw new ArgumentNullException(nameof(renderAction));
        }
    }
}
