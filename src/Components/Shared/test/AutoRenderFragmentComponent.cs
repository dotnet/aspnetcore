// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Test.Helpers
{
    public class AutoRenderFragmentComponent : AutoRenderComponent
    {
        private readonly RenderFragment _renderFragment;

        public AutoRenderFragmentComponent(RenderFragment renderFragment)
        {
            _renderFragment = renderFragment;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
            => _renderFragment(builder);
    }
}
