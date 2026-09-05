// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Rendering;

public class RenderTreeBuilderNullableTest
{
    [Fact]
    public void AddAttribute_NullValueOnOptionElement_EmitsMarkerAndEmptyValueFrame()
    {
        var builder = new RenderTreeBuilder();
        string? nullValue = null;

        builder.OpenElement(0, "option");
        builder.AddAttribute(1, "value", nullValue);
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(
            frames,
            frame => AssertFrame.Element(frame, "option", 3),
            frame => AssertFrame.Attribute(frame, "data-blazor-null-option", "data-blazor-null-option"),
            frame => AssertFrame.Attribute(frame, "value", ""));
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
    public void AddAttribute_NullObjectValueOnOptionElement_EmitsMarkerAndEmptyValueFrame()
    {
        var builder = new RenderTreeBuilder();
        object? nullValue = null;

        builder.OpenElement(0, "option");
        builder.AddAttribute(1, "value", nullValue);
        builder.CloseElement();

        var frames = builder.GetFrames().AsEnumerable().ToArray();
        Assert.Collection(frames,
            frame => AssertFrame.Element(frame, "option", 3),
            frame => AssertFrame.Attribute(frame, "data-blazor-null-option", "data-blazor-null-option"),
            frame => AssertFrame.Attribute(frame, "value", ""));
    }
}
