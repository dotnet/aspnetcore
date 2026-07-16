// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class IsCacheableComponentTest
{
    [Fact]
    public void NoAttribute_IsCacheable()
    {
        Assert.True(CacheViewService.IsCacheableComponent(typeof(ComponentBase), CacheVaryBy.None));
    }

    [Fact]
    public void Attribute_NoVaryBy_IsNotCacheable()
    {
        Assert.False(CacheViewService.IsCacheableComponent(typeof(UnconditionalLiveCachedComponent), CacheVaryBy.None));
        Assert.False(CacheViewService.IsCacheableComponent(typeof(UnconditionalLiveCachedComponent), CacheVaryBy.User));
    }

    [Fact]
    public void Attribute_Throw_ThrowsWhenNotCovered()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CacheViewService.IsCacheableComponent(typeof(ThrowingComponent), CacheVaryBy.None));
    }

    [Fact]
    public void Attribute_VaryBy_NotCacheableWhenNotCovered_CacheableWhenCovered()
    {
        Assert.False(CacheViewService.IsCacheableComponent(typeof(ConditionalLiveCachedComponent), CacheVaryBy.None));
        Assert.True(CacheViewService.IsCacheableComponent(typeof(ConditionalLiveCachedComponent), CacheVaryBy.User));
    }

    [Fact]
    public void Attribute_MultipleVaryByFlags_RequiresFullMatch()
    {
        var partial = CacheVaryBy.User;
        var full = CacheVaryBy.User | CacheVaryBy.Query;

        Assert.False(CacheViewService.IsCacheableComponent(typeof(MultiDimensionLiveCachedComponent), partial));
        Assert.True(CacheViewService.IsCacheableComponent(typeof(MultiDimensionLiveCachedComponent), full));
    }

    [Fact]
    public void Attribute_Inherited_AppliesToSubclass()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CacheViewService.IsCacheableComponent(typeof(DerivedThrowingComponent), CacheVaryBy.None));
    }

    [Fact]
    public void Attribute_ThrowWithMultiFlagCondition_MessageFormatsFlagsAsValidCSharp()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            CacheViewService.IsCacheableComponent(typeof(ThrowingMultiConditionComponent), CacheVaryBy.User));

        Assert.Contains("[CacheCondition(CacheVaryBy.Query | CacheVaryBy.User)]", ex.Message);
        Assert.DoesNotContain("CacheVaryBy.Query, User", ex.Message);
    }

    [CacheBehavior(CacheBehavior.Rerender)]
    private class UnconditionalLiveCachedComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    [CacheBehavior(CacheBehavior.Throw)]
    private class ThrowingComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    private sealed class DerivedThrowingComponent : ThrowingComponent { }

    [CacheBehavior(CacheBehavior.Throw)]
    [CacheCondition(CacheVaryBy.User | CacheVaryBy.Query)]
    private class ThrowingMultiConditionComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    [CacheCondition(CacheVaryBy.User)]
    private class ConditionalLiveCachedComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    [CacheCondition(CacheVaryBy.User | CacheVaryBy.Query)]
    private class MultiDimensionLiveCachedComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }
}
