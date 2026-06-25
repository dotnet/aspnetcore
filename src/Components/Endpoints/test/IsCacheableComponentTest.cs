// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class IsCacheableComponentTest
{
    [Fact]
    public void NoAttribute_IsCacheable()
    {
        Assert.True(CacheBoundaryService.IsCacheableComponent(typeof(ComponentBase), CacheBoundaryVaryBy.None));
    }

    [Fact]
    public void Attribute_NoVaryBy_IsNotCacheable()
    {
        Assert.False(CacheBoundaryService.IsCacheableComponent(typeof(UnconditionalHole), CacheBoundaryVaryBy.None));
        Assert.False(CacheBoundaryService.IsCacheableComponent(typeof(UnconditionalHole), CacheBoundaryVaryBy.User));
    }

    [Fact]
    public void Attribute_Throw_ThrowsWhenNotCovered()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CacheBoundaryService.IsCacheableComponent(typeof(ThrowingComponent), CacheBoundaryVaryBy.None));
    }

    [Fact]
    public void Attribute_VaryBy_NotCacheableWhenNotCovered_CacheableWhenCovered()
    {
        Assert.False(CacheBoundaryService.IsCacheableComponent(typeof(ConditionalHole), CacheBoundaryVaryBy.None));
        Assert.True(CacheBoundaryService.IsCacheableComponent(typeof(ConditionalHole), CacheBoundaryVaryBy.User));
    }

    [Fact]
    public void Attribute_MultipleVaryByFlags_RequiresFullMatch()
    {
        var partial = CacheBoundaryVaryBy.User;
        var full = CacheBoundaryVaryBy.User | CacheBoundaryVaryBy.Query;

        Assert.False(CacheBoundaryService.IsCacheableComponent(typeof(MultiDimensionHole), partial));
        Assert.True(CacheBoundaryService.IsCacheableComponent(typeof(MultiDimensionHole), full));
    }

    [Fact]
    public void Attribute_Inherited_AppliesToSubclass()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CacheBoundaryService.IsCacheableComponent(typeof(DerivedThrowingComponent), CacheBoundaryVaryBy.None));
    }

    [CacheBoundaryPolicy]
    private class UnconditionalHole : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    [CacheBoundaryPolicy(Disallow = true)]
    private class ThrowingComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    private sealed class DerivedThrowingComponent : ThrowingComponent { }

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
}
