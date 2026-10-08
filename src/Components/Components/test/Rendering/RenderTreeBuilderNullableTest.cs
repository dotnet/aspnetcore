// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Rendering;

public class RenderTreeBuilderNullableTest
{
    [Fact]
    public void AddAttribute_NullValueOnStandaloneOptionElement_OnlyTracksName()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "option");
        builder.AddAttribute(1, "value", nullValue);
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(frames, frame => AssertFrame.Element(frame, "option", 1));
    }

    [Fact]
    public void AddAttribute_NullValueOnNonOptionElement_OnlyTracksName()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "input");
        builder.AddAttribute(1, "value", nullValue);
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(frames, frame => AssertFrame.Element(frame, "input", 1));
    }

    [Fact]
    public void AddAttribute_NullObjectValueOnOptionElementInsideSingleSelect_EmitsMarkerAndEmptyValueFrame()
    {
        var builder = new RenderTreeBuilder();
        object? nullValue = null;

        builder.OpenElement(0, "select");
        builder.OpenElement(1, "option");
        builder.AddAttribute(2, "value", nullValue);
        builder.CloseElement();
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(frames,
            frame => AssertFrame.Element(frame, "select", 4),
            frame => AssertFrame.Element(frame, "option", 3),
            frame => AssertFrame.Attribute(frame, "data-blazor-null-option", "data-blazor-null-option"),
            frame => AssertFrame.Attribute(frame, "value", ""));
    }

    [Fact]
    public void AddMultipleAttributes_NullOptionValueOverridesEarlierValue_EmitsMarkerAndEmptyValueFrame()
    {
        var builder = new RenderTreeBuilder();

        builder.OpenElement(0, "select");
        builder.OpenElement(1, "option");
        builder.AddAttribute(2, "value", "earlier-value");
        builder.AddMultipleAttributes(3, new Dictionary<string, object>
        {
            ["value"] = null!,
        });
        builder.CloseElement();
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "select", 4),
            frame => AssertFrame.Element(frame, "option", 3),
            frame => AssertFrame.Attribute(frame, "data-blazor-null-option", "data-blazor-null-option"),
            frame => AssertFrame.Attribute(frame, "value", ""));
    }

    [Fact]
    public void AddMultipleAttributes_ValueOverridesEarlierNullOptionValue_RemovesMarker()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "select");
        builder.OpenElement(1, "option");
        builder.AddAttribute(2, "value", nullValue);
        builder.AddMultipleAttributes(3, new Dictionary<string, object>
        {
            ["value"] = "actual-value",
        });
        builder.CloseElement();
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "select", 3),
            frame => AssertFrame.Element(frame, "option", 2),
            frame => AssertFrame.Attribute(frame, "value", "actual-value"));
    }

    [Fact]
    public void AddAttribute_NullValueOnOptionElementInsideMultipleSelect_OnlyTracksName()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "select");
        builder.AddAttribute(1, "multiple", true);
        builder.OpenElement(2, "option");
        builder.AddAttribute(3, "value", nullValue);
        builder.CloseElement();
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "select", 3),
            frame => AssertFrame.Attribute(frame, "multiple", true),
            frame => AssertFrame.Element(frame, "option", 1));
    }

    [Fact]
    public void AddAttribute_NullValueOnOptionElementInsideOptgroupInMultipleSelect_OnlyTracksName()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "select");
        builder.AddAttribute(1, "multiple", true);
        builder.OpenElement(2, "optgroup");
        builder.OpenElement(3, "option");
        builder.AddAttribute(4, "value", nullValue);
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "select", 4),
            frame => AssertFrame.Attribute(frame, "multiple", true),
            frame => AssertFrame.Element(frame, "optgroup", 2),
            frame => AssertFrame.Element(frame, "option", 1));
    }

    [Fact]
    public void AddAttribute_NullValueOnOptionElementInsideRenderFragmentInMultipleSelect_OnlyTracksName()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "select");
        builder.AddAttribute(1, "multiple", true);
        builder.AddContent(2, fragmentBuilder =>
        {
            fragmentBuilder.OpenElement(0, "option");
            fragmentBuilder.AddAttribute(1, "value", nullValue);
            fragmentBuilder.CloseElement();
        });
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "select", 4),
            frame => AssertFrame.Attribute(frame, "multiple", true),
            frame => AssertFrame.Region(frame, 2),
            frame => AssertFrame.Element(frame, "option", 1));
    }

    [Fact]
    public void AddAttribute_NullValueOnOptionElementInsideSingleSelect_EmitsMarkerAndEmptyValueFrame()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "select");
        builder.OpenElement(1, "option");
        builder.AddAttribute(2, "value", nullValue);
        builder.CloseElement();
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "select", 4),
            frame => AssertFrame.Element(frame, "option", 3),
            frame => AssertFrame.Attribute(frame, "data-blazor-null-option", "data-blazor-null-option"),
            frame => AssertFrame.Attribute(frame, "value", ""));
    }

    [Fact]
    public void AddAttribute_NullValueOnOptionElementWithMixedCasingInsideSingleSelect_EmitsMarkerAndEmptyValueFrame()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "SELECT");
        builder.OpenElement(1, "OPTION");
        builder.AddAttribute(2, "VALUE", nullValue);
        builder.CloseElement();
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "SELECT", 4),
            frame => AssertFrame.Element(frame, "OPTION", 3),
            frame => AssertFrame.Attribute(frame, "data-blazor-null-option", "data-blazor-null-option"),
            frame => AssertFrame.Attribute(frame, "VALUE", ""));
    }

    [Fact]
    public void AddAttribute_NullValueOnOptionElementInsideOptgroupInSingleSelect_EmitsMarkerAndEmptyValueFrame()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "select");
        builder.OpenElement(1, "optgroup");
        builder.OpenElement(2, "option");
        builder.AddAttribute(3, "value", nullValue);
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "select", 5),
            frame => AssertFrame.Element(frame, "optgroup", 4),
            frame => AssertFrame.Element(frame, "option", 3),
            frame => AssertFrame.Attribute(frame, "data-blazor-null-option", "data-blazor-null-option"),
            frame => AssertFrame.Attribute(frame, "value", ""));
    }

    [Fact]
    public void AddAttribute_NullValueOnOptionElementInsideRenderFragmentInSingleSelect_EmitsMarkerAndEmptyValueFrame()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "select");
        builder.AddContent(1, fragmentBuilder =>
        {
            fragmentBuilder.OpenElement(0, "option");
            fragmentBuilder.AddAttribute(1, "value", nullValue);
            fragmentBuilder.CloseElement();
        });
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "select", 5),
            frame => AssertFrame.Region(frame, 4),
            frame => AssertFrame.Element(frame, "option", 3),
            frame => AssertFrame.Attribute(frame, "data-blazor-null-option", "data-blazor-null-option"),
            frame => AssertFrame.Attribute(frame, "value", ""));
    }

    [Fact]
    public void AddAttribute_NullValueOnOptionElementInsideDatalist_OnlyTracksName()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "datalist");
        builder.OpenElement(1, "option");
        builder.AddAttribute(2, "value", nullValue);
        builder.CloseElement();
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "datalist", 2),
            frame => AssertFrame.Element(frame, "option", 1));
    }

    [Fact]
    public void AddAttribute_NullValueOnOptionElementInsideRenderFragmentInDatalist_OnlyTracksName()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "datalist");
        builder.AddContent(1, fragmentBuilder =>
        {
            fragmentBuilder.OpenElement(0, "option");
            fragmentBuilder.AddAttribute(1, "value", nullValue);
            fragmentBuilder.CloseElement();
        });
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "datalist", 3),
            frame => AssertFrame.Region(frame, 2),
            frame => AssertFrame.Element(frame, "option", 1));
    }

}
