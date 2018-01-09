// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.Components;
using Microsoft.Blazor.RenderTree;

namespace BasicTestApp
{
    public class TextOnlyComponent : IComponent
    {
        public void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddText($"Hello from {nameof(TextOnlyComponent)}");
        }
    }
}
