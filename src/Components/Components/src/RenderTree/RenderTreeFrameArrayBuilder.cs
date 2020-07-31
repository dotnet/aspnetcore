// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// A special subclass of <see cref="ArrayBuilder{T}"/> that contains methods optimized for appending <see cref="RenderTreeFrame"/> entries.
    /// </summary>
    internal class RenderTreeFrameArrayBuilder : ArrayBuilder<RenderTreeFrame>
    {
        // You may notice a repeated block at the top of each of these methods. This is intentionally inlined into each
        // method because doing so improves intensive rendering scenarios by around 1% (based on the FastGrid benchmark).
        //
        // The reason it's considered safe to mutate the existing buffer entries in place without replacing them with
        // new struct instances is that the buffer entries should always be blank at the time we are appending, because
        // RenderTreeBuilder always calls Array.Clear on the used portion of the buffer before returning it to the pool.
        // Likewise, if it ever removes entries, it always sets them back to default, rather than just updating indices
        // elsewhere and leaving behind orphaned records. This is necessary both for GC to function correctly (e.g.,
        // because RenderTreeFrame fields may point to other objects) and for safety since the memory can later be reused.

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
