// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Test.Helpers
{
    public abstract class AutoRenderComponent : IComponent
    {
        private RenderHandle _renderHandle;

        public void Init(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public virtual void SetParameters(ParameterCollection parameters)
        {
            parameters.SetParameterProperties(this);
            TriggerRender();
        }

        public void TriggerRender()
            => _renderHandle.Render(BuildRenderTree);

        protected abstract void BuildRenderTree(RenderTreeBuilder builder);
    }
}
