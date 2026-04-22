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
    public void EditForm_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(EditForm), DefaultVaryBy));
    }

    [Fact]
    public void ValidationSummary_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(ValidationSummary), DefaultVaryBy));
    }

    [Fact]
    public void ValidationMessage_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(ValidationMessage<string>), DefaultVaryBy));
    }

    [Fact]
    public void InputText_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(InputText), DefaultVaryBy));
    }

    [Fact]
    public void InputNumber_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(InputNumber<int>), DefaultVaryBy));
    }

    [Fact]
    public void InputCheckbox_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(InputCheckbox), DefaultVaryBy));
    }

    [Fact]
    public void AntiforgeryToken_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(AntiforgeryToken), DefaultVaryBy));
    }

    [Fact]
    public void NotCacheBoundary_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(NotCacheBoundary), DefaultVaryBy));
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
    public void SectionOutlet_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(SectionOutlet), DefaultVaryBy));
    }

    [Fact]
    public void SectionContent_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(SectionContent), DefaultVaryBy));
    }

    [Fact]
    public void AuthorizeView_IsHole_WhenNotVaryByUser()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(AuthorizeView), DefaultVaryBy));
    }

    [Fact]
    public void AuthorizeView_IsNotHole_WhenVaryByUser()
    {
        Assert.False(EndpointHtmlRenderer.IsHoleComponent(typeof(AuthorizeView), VaryByUser));
    }

    [Fact]
    public void AuthorizeViewSubclass_IsHole_WhenNotVaryByUser()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(CustomAuthorizeView), DefaultVaryBy));
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

    [Fact]
    public void CustomComponent_IsNotHole()
    {
        Assert.False(EndpointHtmlRenderer.IsHoleComponent(typeof(PlainComponent), DefaultVaryBy));
    }

    [Fact]
    public void CustomInputBaseDescendant_IsHole()
    {
        Assert.True(EndpointHtmlRenderer.IsHoleComponent(typeof(CustomInput), DefaultVaryBy));
    }

    private class PlainComponent : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    }

    private class CustomAuthorizeView : AuthorizeView { }

    private class CustomInput : InputText { }
}
