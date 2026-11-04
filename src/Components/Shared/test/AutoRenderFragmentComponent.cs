// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Test.Helpers;

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
