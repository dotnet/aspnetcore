// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Test
{
    public abstract class AutoRenderComponent : IComponent
    {
        private RenderHandle _renderHandle;

        public abstract void BuildRenderTree(RenderTreeBuilder builder);

        public void Init(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public void SetParameters(ParameterCollection parameters)
        {
            parameters.AssignToProperties(this);
            _renderHandle.Render();
        }
    }
}
