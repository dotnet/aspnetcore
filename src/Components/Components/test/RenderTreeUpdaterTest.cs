// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Test;

public class RenderTreeUpdaterTest
{
    [Fact]
    public void IgnoresUnknownEventHandlerId()
    {
        // Arrange
        var valuePropName = "testprop";
        var renderer = new TestRenderer();
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "elem");
        builder.AddAttribute(1, "eventname", (Action)(() => { }));
        builder.SetUpdatesAttributeName(valuePropName);
        builder.AddAttribute(2, valuePropName, "initial value");
        builder.CloseElement();
        var frames = builder.GetFrames();
        frames.Array[1] = frames.Array[1].WithAttributeEventHandlerId(123); // An unrelated event

        // Act
        RenderTreeUpdater.UpdateToMatchClientState(builder, 456, "new value");

        // Assert
        Assert.Collection(frames.AsEnumerable(),
            frame => AssertFrame.Element(frame, "elem", 3, 0),
            frame => AssertFrame.Attribute(frame, "eventname", v => Assert.IsType<Action>(v), 1),
            frame => AssertFrame.Attribute(frame, valuePropName, "initial value", 2));
    }

    [Fact]
    public void IgnoresUpdatesToAttributesIfUnexpectedValueTypeSupplied()
    {
        // Currently we only allow the client to supply a string or a bool, since those are the
        // only types of values we render onto attributes

        // Arrange
        var valuePropName = "testprop";
        var renderer = new TestRenderer();
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "elem");
        builder.AddAttribute(1, "eventname", (Action)(() => { }));
        builder.SetUpdatesAttributeName(valuePropName);
        builder.AddAttribute(2, valuePropName, "initial value");
        builder.CloseElement();
        var frames = builder.GetFrames();
        frames.Array[1] = frames.Array[1].WithAttributeEventHandlerId(123); // An unrelated event

        // Act
        RenderTreeUpdater.UpdateToMatchClientState(builder, 123, new object());

        // Assert
        Assert.Collection(frames.AsEnumerable(),
            frame => AssertFrame.Element(frame, "elem", 3, 0),
            frame => AssertFrame.Attribute(frame, "eventname", v => Assert.IsType<Action>(v), 1),
            frame => AssertFrame.Attribute(frame, valuePropName, "initial value", 2));
    }

    [Fact]
    public void UpdatesOnlyMatchingAttributeValue()
    {
        // Arrange
        var valuePropName = "testprop";
        var renderer = new TestRenderer();
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "elem");
        builder.AddAttribute(1, "eventname", (Action)(() => { }));
        builder.SetUpdatesAttributeName(valuePropName);
        builder.AddAttribute(2, valuePropName, "unchanged 1");
        builder.CloseElement();
        builder.OpenElement(3, "elem");
        builder.AddAttribute(4, "eventname", (Action)(() => { }));
        builder.SetUpdatesAttributeName(valuePropName);
        builder.AddAttribute(5, "unrelated prop before", "unchanged 2");
        builder.AddAttribute(6, valuePropName, "initial value");
        builder.AddAttribute(7, "unrelated prop after", "unchanged 3");
        builder.CloseElement();
        var frames = builder.GetFrames();
        frames.Array[1] = frames.Array[1].WithAttributeEventHandlerId(123); // An unrelated event
        frames.Array[4] = frames.Array[4].WithAttributeEventHandlerId(456);

        // Act
        RenderTreeUpdater.UpdateToMatchClientState(builder, 456, "new value");

        // Assert
        Assert.Collection(frames.AsEnumerable(),
            frame => AssertFrame.Element(frame, "elem", 3, 0),
            frame => AssertFrame.Attribute(frame, "eventname", v => Assert.IsType<Action>(v), 1),
            frame => AssertFrame.Attribute(frame, valuePropName, "unchanged 1", 2),
            frame => AssertFrame.Element(frame, "elem", 5, 3),
            frame => AssertFrame.Attribute(frame, "eventname", v => Assert.IsType<Action>(v), 4),
            frame => AssertFrame.Attribute(frame, "unrelated prop before", "unchanged 2", 5),
            frame => AssertFrame.Attribute(frame, valuePropName, "new value", 6),
            frame => AssertFrame.Attribute(frame, "unrelated prop after", "unchanged 3", 7));
    }

    [Fact]
    public void AddsAttributeIfNotFound()
    {
        // Arrange
        var valuePropName = "testprop";
        var renderer = new TestRenderer();
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "elem");
        builder.AddAttribute(1, "eventname", (Action)(() => { }));
        builder.SetUpdatesAttributeName(valuePropName);
        builder.CloseElement();
        var frames = builder.GetFrames();
        frames.Array[1] = frames.Array[1].WithAttributeEventHandlerId(123);

        // Act
        RenderTreeUpdater.UpdateToMatchClientState(builder, 123, "new value");
        frames = builder.GetFrames();

        // Assert
        Assert.Collection(frames.AsEnumerable(),
            frame => AssertFrame.Element(frame, "elem", 3, 0),
            frame => AssertFrame.Attribute(frame, valuePropName, "new value", RenderTreeDiffBuilder.SystemAddedAttributeSequenceNumber),
            frame => AssertFrame.Attribute(frame, "eventname", v => Assert.IsType<Action>(v), 1));
    }

    [Fact]
    public void OmitsAttributeIfNotFoundButValueIsOmissible()
    {
        // Arrange
        var valuePropName = "testprop";
        var renderer = new TestRenderer();
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "elem");
        builder.AddAttribute(1, "eventname", (Action)(() => { }));
        builder.SetUpdatesAttributeName(valuePropName);
        builder.CloseElement();
        var frames = builder.GetFrames();
        frames.Array[1] = frames.Array[1].WithAttributeEventHandlerId(123);

        // Act
        RenderTreeUpdater.UpdateToMatchClientState(builder, 123, false);
        frames = builder.GetFrames();

        // Assert
        Assert.Collection(frames.AsEnumerable(),
            frame => AssertFrame.Element(frame, "elem", 2, 0),
            frame => AssertFrame.Attribute(frame, "eventname", v => Assert.IsType<Action>(v), 1));
    }

    [Fact]
    public void ExpandsAllAncestorsWhenAddingAttribute()
    {
        // Arrange
        var valuePropName = "testprop";
        var renderer = new TestRenderer();
        var builder = new RenderTreeBuilder();
        builder.OpenElement(0, "grandparent");
        builder.OpenRegion(1);
        builder.OpenElement(2, "sibling before"); // To show that non-ancestors aren't expanded
        builder.CloseElement();
        builder.OpenElement(3, "elem with handler");
        builder.AddAttribute(4, "eventname", (Action)(() => { }));
        builder.SetUpdatesAttributeName(valuePropName);
        builder.CloseElement(); // elem with handler
        builder.CloseRegion();
        builder.CloseElement(); // grandparent
        var frames = builder.GetFrames();
        frames.Array[4] = frames.Array[4].WithAttributeEventHandlerId(123);

        // Act
        RenderTreeUpdater.UpdateToMatchClientState(builder, 123, "new value");
        frames = builder.GetFrames();

        // Assert
        Assert.Collection(frames.AsEnumerable(),
            frame => AssertFrame.Element(frame, "grandparent", 6, 0),
            frame => AssertFrame.Region(frame, 5, 1),
            frame => AssertFrame.Element(frame, "sibling before", 1, 2),
            frame => AssertFrame.Element(frame, "elem with handler", 3, 3),
            frame => AssertFrame.Attribute(frame, valuePropName, "new value", RenderTreeDiffBuilder.SystemAddedAttributeSequenceNumber),
            frame => AssertFrame.Attribute(frame, "eventname", v => Assert.IsType<Action>(v), 4));
    }

    private static ArrayRange<RenderTreeFrame> BuildFrames(params RenderTreeFrame[] frames)
        => new ArrayRange<RenderTreeFrame>(frames, frames.Length);
}
