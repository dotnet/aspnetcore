// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Components.RenderTree
{
    /// <summary>
    /// A special subclass of <see cref="ArrayBuilder{T}"/> that contains methods optimized for appending <see cref="RenderTreeEdit"/> entries.
    /// </summary>
    internal class RenderTreeEditArrayBuilder : ArrayBuilder<RenderTreeEdit>
    {
        public RenderTreeEditArrayBuilder(int minCapacity, ArrayPool<RenderTreeEdit>? arrayPool) : base(minCapacity, arrayPool)
        {
        }

        public void AppendPermutationListEntry(int oldSiblingIndex, int newSiblingIndex)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.Type == default);

            item.Type = RenderTreeEditType.PermutationListEntry;
            item.SiblingIndex = oldSiblingIndex;
            item.MoveToSiblingIndex = newSiblingIndex;
        }

        public void AppendPermutationListEnd()
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.Type == default);

            item.Type = RenderTreeEditType.PermutationListEnd;
        }

        public void AppendUpdateText(int siblingIndex, int referenceFrameIndex)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.Type == default);

            item.Type = RenderTreeEditType.UpdateText;
            item.SiblingIndex = siblingIndex;
            item.ReferenceFrameIndex = referenceFrameIndex;
        }

        public void AppendUpdateMarkup(int siblingIndex, int referenceFrameIndex)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.Type == default);

            item.Type = RenderTreeEditType.UpdateMarkup;
            item.SiblingIndex = siblingIndex;
            item.ReferenceFrameIndex = referenceFrameIndex;
        }

        public void AppendStepIn(int siblingIndex)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.Type == default);

            item.Type = RenderTreeEditType.StepIn;
            item.SiblingIndex = siblingIndex;
        }

        public void AppendStepOut()
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.Type == default);

            item.Type = RenderTreeEditType.StepOut;
        }

        public void AppendSetAttribute(int siblingIndex, int referenceFrameIndex)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.Type == default);

            item.Type = RenderTreeEditType.SetAttribute;
            item.SiblingIndex = siblingIndex;
            item.ReferenceFrameIndex = referenceFrameIndex;
        }

        public void AppendPrependFrame(int siblingIndex, int referenceFrameIndex)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.Type == default);

            item.Type = RenderTreeEditType.PrependFrame;
            item.SiblingIndex = siblingIndex;
            item.ReferenceFrameIndex = referenceFrameIndex;
        }

        public void AppendRemoveAttribute(int siblingIndex, string attributeName)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.Type == default);

            item.Type = RenderTreeEditType.RemoveAttribute;
            item.SiblingIndex = siblingIndex;
            item.RemovedAttributeName = attributeName;
        }

        public void AppendRemoveFrame(int siblingIndex)
        {
            GrowBufferIfFull();
            ref var item = ref _items[_itemsInUse++];
            Debug.Assert(item.Type == default);

            item.Type = RenderTreeEditType.RemoveFrame;
            item.SiblingIndex = siblingIndex;
        }
    }
}
