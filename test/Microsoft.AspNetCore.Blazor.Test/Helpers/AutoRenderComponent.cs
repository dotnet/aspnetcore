// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Test.Helpers
{
    public abstract class AutoRenderComponent : IComponent
    {
        private RenderHandle _renderHandle;

        public void Init(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public void SetParameters(ParameterCollection parameters)
        {
            parameters.AssignToProperties(this);
            TriggerRender();
        }

        public void TriggerRender()
            => _renderHandle.Render(BuildRenderTree);

        protected abstract void BuildRenderTree(RenderTreeBuilder builder);
    }
}
