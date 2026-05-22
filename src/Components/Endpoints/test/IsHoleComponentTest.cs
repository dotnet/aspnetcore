// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class IsHoleComponentTest
{
    [Fact]
    public void NoAttribute_IsNotHole()
    {
        Assert.False(EndpointHtmlRenderer.IsHoleComponent(typeof(ComponentBase), CacheBoundaryVaryBy.None));
    }

    [Fact]
    public void Attribute_NoVaryBy_IsUnconditionalHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(UnconditionalHole), CacheBoundaryVaryBy.None));
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(UnconditionalHole), CacheBoundaryVaryBy.User));
    }

    [Fact]
    public void Attribute_Throw_ThrowsWhenNotCovered()
    {
        Assert.Throws<InvalidOperationException>(() =>
            EndpointHtmlRenderer.IsHoleComponent(typeof(ThrowingComponent), CacheBoundaryVaryBy.None));
    }

    [Fact]
    public void Attribute_VaryBy_IsHoleWhenNotCovered_SafeWhenCovered()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(ConditionalHole), CacheBoundaryVaryBy.None));
        Assert.False(EndpointHtmlRenderer.IsHoleComponent(typeof(ConditionalHole), CacheBoundaryVaryBy.User));
    }

    [Fact]
    public void Attribute_MultipleVaryByFlags_RequiresFullMatch()
    {
        var partial = CacheBoundaryVaryBy.User;
        var full = CacheBoundaryVaryBy.User | CacheBoundaryVaryBy.Query;

        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(MultiDimensionHole), partial));
        Assert.False(EndpointHtmlRenderer.IsHoleComponent(typeof(MultiDimensionHole), full));
    }

    [Fact]
    public void Attribute_Inherited_AppliesToSubclass()
    {
        Assert.Throws<InvalidOperationException>(() =>
            EndpointHtmlRenderer.IsHoleComponent(typeof(CustomInput), CacheBoundaryVaryBy.None));
    }

    [CacheBoundaryPolicy]
    private class UnconditionalHole : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    [CacheBoundaryPolicy(Throw = true)]
    private class ThrowingComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    [CacheBoundaryPolicy(VaryBy = CacheBoundaryVaryBy.User)]
    private class ConditionalHole : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    [CacheBoundaryPolicy(VaryBy = CacheBoundaryVaryBy.User | CacheBoundaryVaryBy.Query)]
    private class MultiDimensionHole : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    private class CustomInput : InputText { }
}
