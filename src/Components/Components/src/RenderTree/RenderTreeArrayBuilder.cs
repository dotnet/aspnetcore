// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// A special subclass of <see cref="ArrayBuilder{T}"/> that contains methods optimized for appending <see cref="RenderTreeFrame"/> entries.
    /// </summary>
    internal class RenderTreeArrayBuilder : ArrayBuilder<RenderTreeFrame>
    {
        public void AppendElement(int sequence, string elementName)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Element;
            item.ElementName = elementName;
        }

        public void AppendMarkup(int sequence, string markupContent)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];

            item.Sequence = sequence;
            item.FrameType = RenderTreeFrameType.Markup;
            item.MarkupContent = markupContent;
        }
    }
}
