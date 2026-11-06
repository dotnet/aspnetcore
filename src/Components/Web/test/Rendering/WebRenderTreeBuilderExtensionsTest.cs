// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Web;

public class WebRenderTreeBuilderExtensionsTest
{
    [Fact]
    public void AddEventDebounceAttribute_PositiveValue_AppendsInternalAttributeFrame()
    {
        var builder = new RenderTreeBuilder();

        builder.OpenElement(0, "input");
        builder.AddEventDebounceAttribute(1, "oninput", 300);
        builder.CloseElement();

        var frames = builder.GetFrames();
        Assert.Collection(
            frames.AsEnumerable(),
            frame => AssertFrame.Element(frame, "input", 2, 0),
            frame => AssertFrame.Attribute(frame, "__internal_debounce_oninput", "300", 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void AddEventDebounceAttribute_NonPositiveValue_OmitsAttributeFrame(int value)
    {
        var builder = new RenderTreeBuilder();

        builder.OpenElement(0, "input");
        builder.AddEventDebounceAttribute(1, "oninput", value);
        builder.CloseElement();

        var frames = builder.GetFrames();
        Assert.Collection(
            frames.AsEnumerable(),
            frame => AssertFrame.Element(frame, "input", 1, 0));
    }

    [Fact]
    public void AddEventThrottleAttribute_LeadingMode_AppendsIntervalFrame()
    {
        var builder = new RenderTreeBuilder();

        builder.OpenElement(0, "button");
        builder.AddEventThrottleAttribute(1, "onclick", 500);
        builder.CloseElement();

        var frames = builder.GetFrames();
        Assert.Collection(
            frames.AsEnumerable(),
            frame => AssertFrame.Element(frame, "button", 2, 0),
            frame => AssertFrame.Attribute(frame, "__internal_throttle_onclick", "500", 1));
    }

    [Fact]
    public void AddEventThrottleAttribute_LeadingAndTrailingMode_AppendsTrailingIntervalFrame()
    {
        var builder = new RenderTreeBuilder();

        builder.OpenElement(0, "div");
        builder.AddEventThrottleAttribute(1, "onscroll", 100, EventThrottleMode.LeadingAndTrailing);
        builder.CloseElement();

        var frames = builder.GetFrames();
        Assert.Collection(
            frames.AsEnumerable(),
            frame => AssertFrame.Element(frame, "div", 2, 0),
            frame => AssertFrame.Attribute(frame, "__internal_throttle_onscroll", "100:trailing", 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void AddEventThrottleAttribute_NonPositiveValue_OmitsAttributeFrame(int value)
    {
        var builder = new RenderTreeBuilder();

        builder.OpenElement(0, "div");
        builder.AddEventThrottleAttribute(1, "onscroll", value, EventThrottleMode.LeadingAndTrailing);
        builder.CloseElement();

        var frames = builder.GetFrames();
        Assert.Collection(
            frames.AsEnumerable(),
            frame => AssertFrame.Element(frame, "div", 1, 0));
    }
}
