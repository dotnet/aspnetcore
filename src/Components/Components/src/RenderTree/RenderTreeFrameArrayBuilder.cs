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

        public void AppendElement(int sequence, string elementName)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            _items[_itemsInUse++] = new RenderTreeFrame
            {
                Sequence = sequence,
                FrameType = RenderTreeFrameType.Element,
                ElementName = elementName,
            };
        }

        public void AppendText(int sequence, string textContent)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            _items[_itemsInUse++] = new RenderTreeFrame
            {
                Sequence = sequence,
                FrameType = RenderTreeFrameType.Text,
                TextContent = textContent,
            };
        }

        public void AppendMarkup(int sequence, string markupContent)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            _items[_itemsInUse++] = new RenderTreeFrame
            {
                Sequence = sequence,
                FrameType = RenderTreeFrameType.Markup,
                MarkupContent = markupContent,
            };
        }

        public void AppendAttribute(int sequence, string attributeName, object? attributeValue)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }

            _items[_itemsInUse++] = new RenderTreeFrame
            {
                Sequence = sequence,
                FrameType = RenderTreeFrameType.Attribute,
                AttributeName = attributeName,
                AttributeValue = attributeValue,
            };
        }

        public void AppendComponent(int sequence, Type componentType)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }
            
            _items[_itemsInUse++] = new RenderTreeFrame
            {
                Sequence = sequence,
                FrameType = RenderTreeFrameType.Component,
                ComponentType = componentType,
            };
        }

        public void AppendElementReferenceCapture(int sequence, Action<ElementReference> elementReferenceCaptureAction)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }
            
            _items[_itemsInUse++] = new RenderTreeFrame
            {
                Sequence = sequence,
                FrameType = RenderTreeFrameType.ElementReferenceCapture,
                ElementReferenceCaptureAction = elementReferenceCaptureAction,
            };
        }

        public void AppendComponentReferenceCapture(int sequence, Action<object?> componentReferenceCaptureAction, int parentFrameIndexValue)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }
            
            _items[_itemsInUse++] = new RenderTreeFrame
            {
                Sequence = sequence,
                FrameType = RenderTreeFrameType.ComponentReferenceCapture,
                ComponentReferenceCaptureAction = componentReferenceCaptureAction,
                ComponentReferenceCaptureParentFrameIndex = parentFrameIndexValue,
            };
        }

        public void AppendRegion(int sequence)
        {
            if (_itemsInUse == _items.Length)
            {
                GrowBuffer(_items.Length * 2);
            }
            
            _items[_itemsInUse++] = new RenderTreeFrame
            {
                Sequence = sequence,
                FrameType = RenderTreeFrameType.Region,
            };
        }
    }
}
