// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// A special subclass of <see cref="ArrayBuilder{T}"/> that contains methods optimized for appending <see cref="RenderTreeFrame"/> entries.
    /// </summary>
    internal class RenderTreeFrameArrayBuilder : ArrayBuilder<RenderTreeFrame>
    {
        public void AppendElement(int sequence, string elementName)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Element;
            item.ElementName = elementName;
        }

        public void AppendText(int sequence, string textContent)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Text;
            item.TextContent = textContent;
        }

        public void AppendMarkup(int sequence, string markupContent)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Markup;
            item.MarkupContent = markupContent;
        }

        public void AppendAttribute(int sequence, string attributeName, object? attributeValue)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Attribute;
            item.AttributeName = attributeName;
            item.AttributeValue = attributeValue;
        }

        public void AppendComponent(int sequence, Type componentType)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Component;
            item.ComponentType = componentType;
        }

        public void AppendElementReferenceCapture(int sequence, Action<ElementReference> elementReferenceCaptureAction)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.ElementReferenceCapture;
            item.ElementReferenceCaptureAction = elementReferenceCaptureAction;
        }

        public void AppendComponentReferenceCapture(int sequence, Action<object?> componentReferenceCaptureAction, int parentFrameIndexValue)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.ComponentReferenceCapture;
            item.ComponentReferenceCaptureAction = componentReferenceCaptureAction;
            item.ComponentReferenceCaptureParentFrameIndex = parentFrameIndexValue;
        }

        public void AppendRegion(int sequence)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Region;
        }
    }
}
