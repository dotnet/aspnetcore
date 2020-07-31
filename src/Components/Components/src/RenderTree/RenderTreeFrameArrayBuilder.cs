// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    // TODO: Make sure there aren't any cases where the underlying buffer contains nonzero data

    /// <summary>
    /// A special subclass of <see cref="ArrayBuilder{T}"/> that contains methods optimized for appending <see cref="RenderTreeFrame"/> entries.
    /// </summary>
    internal class RenderTreeFrameArrayBuilder : ArrayBuilder<RenderTreeFrame>
    {
        public void AppendElement(int sequence, string elementName)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.FrameType == default);

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Element;
            item.ElementName = elementName;
        }

        public void AppendText(int sequence, string textContent)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.FrameType == default);

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Text;
            item.TextContent = textContent;
        }

        public void AppendMarkup(int sequence, string markupContent)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.FrameType == default);

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Markup;
            item.MarkupContent = markupContent;
        }

        public void AppendAttribute(int sequence, string attributeName, object? attributeValue)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.FrameType == default);

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Attribute;
            item.AttributeName = attributeName;
            item.AttributeValue = attributeValue;
        }

        public void AppendComponent(int sequence, Type componentType)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.FrameType == default);

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Component;
            item.ComponentType = componentType;
        }

        public void AppendElementReferenceCapture(int sequence, Action<ElementReference> elementReferenceCaptureAction)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.FrameType == default);

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.ElementReferenceCapture;
            item.ElementReferenceCaptureAction = elementReferenceCaptureAction;
        }

        public void AppendComponentReferenceCapture(int sequence, Action<object?> componentReferenceCaptureAction, int parentFrameIndexValue)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.FrameType == default);

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.ComponentReferenceCapture;
            item.ComponentReferenceCaptureAction = componentReferenceCaptureAction;
            item.ComponentReferenceCaptureParentFrameIndex = parentFrameIndexValue;
        }

        public void AppendRegion(int sequence)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.FrameType == default);

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Region;
        }
    }
}
