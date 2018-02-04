// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Test.Shared
{
    internal static class AssertFrame
    {
        public static void Sequence(RenderTreeFrame frame, int? sequence = null)
        {
            if (sequence.HasValue)
            {
                Assert.Equal(sequence.Value, frame.Sequence);
            }
        }

        public static void Text(RenderTreeFrame frame, string textContent, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.Text, frame.FrameType);
            Assert.Equal(textContent, frame.TextContent);
            Assert.Equal(0, frame.ElementDescendantsEndIndex);
            AssertFrame.Sequence(frame, sequence);
        }

        public static void Element(RenderTreeFrame frame, string elementName, int descendantsEndIndex, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.Element, frame.FrameType);
            Assert.Equal(elementName, frame.ElementName);
            Assert.Equal(descendantsEndIndex, frame.ElementDescendantsEndIndex);
            AssertFrame.Sequence(frame, sequence);
        }

        public static void Attribute(RenderTreeFrame frame, string attributeName, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.Attribute, frame.FrameType);
            Assert.Equal(attributeName, frame.AttributeName);
            AssertFrame.Sequence(frame, sequence);
        }

        public static void Attribute(RenderTreeFrame frame, string attributeName, string attributeValue, int? sequence = null)
        {
            AssertFrame.Attribute(frame, attributeName, sequence);
            Assert.Equal(attributeValue, frame.AttributeValue);
        }

        public static void Attribute(RenderTreeFrame frame, string attributeName, UIEventHandler attributeEventHandlerValue, int? sequence = null)
        {
            AssertFrame.Attribute(frame, attributeName, sequence);
            Assert.Equal(attributeEventHandlerValue, frame.AttributeValue);
        }

        public static void Component<T>(RenderTreeFrame frame, int? sequence = null) where T : IComponent
        {
            Assert.Equal(RenderTreeFrameType.Component, frame.FrameType);
            Assert.Equal(typeof(T), frame.ComponentType);
            AssertFrame.Sequence(frame, sequence);
        }

        public static void Whitespace(RenderTreeFrame frame, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.Text, frame.FrameType);
            AssertFrame.Sequence(frame, sequence);
            Assert.True(string.IsNullOrWhiteSpace(frame.TextContent));
        }
    }
}
