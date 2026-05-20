// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Sections;
using Microsoft.AspNetCore.Components.Web;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class IsHoleComponentTest
{
    private static readonly CacheBoundaryVaryBy DefaultVaryBy = CacheBoundaryVaryBy.None;
    private static readonly CacheBoundaryVaryBy VaryByUser = CacheBoundaryVaryBy.User;

    [Fact]
    public void EditForm_Throws_InsideCacheBoundary()
    {
        Assert.Throws<InvalidOperationException>(() => EndpointHtmlRenderer.IsHoleComponent(typeof(EditForm), DefaultVaryBy));
    }

    [Fact]
    public void CustomInputBaseDescendant_Throws_InsideCacheBoundary()
    {
        Assert.Throws<InvalidOperationException>(() => EndpointHtmlRenderer.IsHoleComponent(typeof(CustomInput), DefaultVaryBy));
    }

    [Fact]
    public void AntiforgeryToken_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(AntiforgeryToken), DefaultVaryBy));
    }

    [Fact]
    public void SSRRenderModeBoundary_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(SSRRenderModeBoundary), DefaultVaryBy));
    }

    [Fact]
    public void HeadOutlet_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(HeadOutlet), DefaultVaryBy));
    }

    [Fact]
    public void SectionOutlet_IsNotHole()
    {
        Assert.False(EndpointHtmlRenderer.IsHoleComponent(typeof(SectionOutlet), DefaultVaryBy));
    }

    [Fact]
    public void AuthorizeView_Throws_WhenNotVaryByUser()
    {
        Assert.Throws<InvalidOperationException>(() => EndpointHtmlRenderer.IsHoleComponent(typeof(AuthorizeView), DefaultVaryBy));
    }

    [Fact]
    public void AuthorizeView_IsNotHole_WhenVaryByUser()
    {
        Assert.False(EndpointHtmlRenderer.IsHoleComponent(typeof(AuthorizeView), VaryByUser));
    }

    [Fact]
    public void AuthorizeViewSubclass_Throws_WhenNotVaryByUser()
    {
        Assert.Throws<InvalidOperationException>(() => EndpointHtmlRenderer.IsHoleComponent(typeof(CustomAuthorizeView), DefaultVaryBy));
    }

    [Fact]
    public void AuthorizeViewSubclass_IsNotHole_WhenVaryByUser()
    {
        Assert.False(EndpointHtmlRenderer.IsHoleComponent(typeof(CustomAuthorizeView), VaryByUser));
    }

    [Fact]
    public void RegularComponent_IsNotHole()
    {
        Assert.False(EndpointHtmlRenderer.IsHoleComponent(typeof(ComponentBase), DefaultVaryBy));
    }

    private class CustomAuthorizeView : AuthorizeView { }

    private class CustomInput : InputText { }

    [Fact]
    public void CacheBoundaryPolicy_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(ExcludedComponent), DefaultVaryBy));
    }

    [Fact]
    public void CacheBoundaryPolicy_WithVaryBy_IsHole_WhenNotVaryByUser()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(ExcludedWithVaryByUser), DefaultVaryBy));
    }

    [Fact]
    public void CacheBoundaryPolicy_WithVaryBy_IsNotHole_WhenVaryByUser()
    {
        Assert.False(EndpointHtmlRenderer.IsHoleComponent(typeof(ExcludedWithVaryByUser), VaryByUser));
    }

    [CacheBoundaryPolicy]
    private class ExcludedComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    [CacheBoundaryPolicy(VaryBy = CacheBoundaryVaryBy.User)]
    private class ExcludedWithVaryByUser : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    [Fact]
    public void CacheBoundaryPolicy_WithMultipleVaryByFlags_IsHole_WhenPartialMatch()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(ExcludedWithVaryByUserAndQuery), VaryByUser));
    }

    [Fact]
    public void CacheBoundaryPolicy_WithMultipleVaryByFlags_IsNotHole_WhenFullMatch()
    {
        var varyBy = CacheBoundaryVaryBy.User | CacheBoundaryVaryBy.Query;
        Assert.False(EndpointHtmlRenderer.IsHoleComponent(typeof(ExcludedWithVaryByUserAndQuery), varyBy));
    }

    [CacheBoundaryPolicy(VaryBy = CacheBoundaryVaryBy.User | CacheBoundaryVaryBy.Query)]
    private class ExcludedWithVaryByUserAndQuery : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }
}
