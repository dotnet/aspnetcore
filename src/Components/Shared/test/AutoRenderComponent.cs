// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Test.Helpers
{
    public abstract class AutoRenderComponent : IComponent
    {
        private RenderHandle _renderHandle;

        public void Configure(RenderHandle renderHandle)
        {
            _renderHandle = renderHandle;
        }

        public virtual Task SetParametersAsync(ParameterCollection parameters)
        {
            parameters.SetParameterProperties(this);
            TriggerRender();
            return Task.CompletedTask;
        }

        public void TriggerRender()
            => _renderHandle.Render(BuildRenderTree);

        protected abstract void BuildRenderTree(RenderTreeBuilder builder);
    }
}
