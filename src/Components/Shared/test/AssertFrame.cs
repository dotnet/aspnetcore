// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test.Helpers
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
            Assert.Equal(0, frame.ElementSubtreeLength);
            AssertFrame.Sequence(frame, sequence);
        }

        internal static void Markup(RenderTreeFrame frame, string markupContent, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.Markup, frame.FrameType);
            Assert.Equal(markupContent, frame.MarkupContent);
            Assert.Equal(0, frame.ElementSubtreeLength);
            AssertFrame.Sequence(frame, sequence);
        }

        public static void Element(RenderTreeFrame frame, string elementName, int subtreeLength, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.Element, frame.FrameType);
            Assert.Equal(elementName, frame.ElementName);
            Assert.Equal(subtreeLength, frame.ElementSubtreeLength);
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

        public static void Attribute(RenderTreeFrame frame, string attributeName, Action<EventArgs> attributeEventHandlerValue, int? sequence = null)
        {
            AssertFrame.Attribute(frame, attributeName, sequence);
            Assert.Equal(attributeEventHandlerValue, frame.AttributeValue);
        }

        public static void Attribute(RenderTreeFrame frame, string attributeName, object attributeValue, int? sequence = null)
        {
            AssertFrame.Attribute(frame, attributeName, sequence);
            Assert.Equal(attributeValue, frame.AttributeValue);
        }

        public static void Attribute(RenderTreeFrame frame, string attributeName, Type valueType, int? sequence = null)
        {
            AssertFrame.Attribute(frame, attributeName, sequence);
            Assert.IsType(valueType, frame.AttributeValue);
        }

        public static void Attribute(RenderTreeFrame frame, string attributeName, Action<object> attributeValidator, int? sequence = null)
        {
            AssertFrame.Attribute(frame, attributeName, sequence);
            attributeValidator(frame.AttributeValue);
        }

        public static void Component<T>(RenderTreeFrame frame, int? subtreeLength = null, int? sequence = null) where T : IComponent
        {
            Component(frame, typeof(T).FullName, subtreeLength, sequence);
        }

        public static void Component(RenderTreeFrame frame, string typeName, int? subtreeLength = null, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.Component, frame.FrameType);
            Assert.Equal(typeName, frame.ComponentType.FullName);
            if (subtreeLength.HasValue)
            {
                Assert.Equal(subtreeLength.Value, frame.ComponentSubtreeLength);
            }
            AssertFrame.Sequence(frame, sequence);
        }

        public static void ComponentWithInstance<T>(RenderTreeFrame frame, int componentId, int? subtreeLength = null, int? sequence = null) where T : IComponent
        {
            AssertFrame.Component<T>(frame, subtreeLength, sequence);
            Assert.IsType<T>(frame.Component);
            Assert.Equal(componentId, frame.ComponentId);
        }

        public static void Region(RenderTreeFrame frame, int subtreeLength, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.Region, frame.FrameType);
            Assert.Equal(subtreeLength, frame.RegionSubtreeLength);
            AssertFrame.Sequence(frame, sequence);
        }

        public static void MarkupWhitespace(RenderTreeFrame frame, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.Markup, frame.FrameType);
            AssertFrame.Sequence(frame, sequence);
            Assert.True(string.IsNullOrWhiteSpace(frame.TextContent));
        }

        public static void TextWhitespace(RenderTreeFrame frame, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.Text, frame.FrameType);
            AssertFrame.Sequence(frame, sequence);
            Assert.True(string.IsNullOrWhiteSpace(frame.TextContent));
        }

        public static void ElementReferenceCapture(RenderTreeFrame frame, Action<ElementReference> action, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.ElementReferenceCapture, frame.FrameType);
            Assert.Same(action, frame.ElementReferenceCaptureAction);
            AssertFrame.Sequence(frame, sequence);
        }

        public static void ComponentReferenceCapture(RenderTreeFrame frame, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.ComponentReferenceCapture, frame.FrameType);
            Assert.NotNull(frame.ComponentReferenceCaptureAction);
            AssertFrame.Sequence(frame, sequence);
        }

        public static void ComponentReferenceCapture(RenderTreeFrame frame, Action<object> action, int? sequence = null)
        {
            Assert.Equal(RenderTreeFrameType.ComponentReferenceCapture, frame.FrameType);
            Assert.Same(action, frame.ComponentReferenceCaptureAction);
            AssertFrame.Sequence(frame, sequence);
        }
    }
}
