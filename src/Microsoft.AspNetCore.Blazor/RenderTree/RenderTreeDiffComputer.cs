// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;

namespace Microsoft.AspNetCore.Blazor.RenderTree
{
    internal class RenderTreeDiffComputer
    {
        private readonly Renderer _renderer;
        private readonly ArrayBuilder<RenderTreeEdit> _entries = new ArrayBuilder<RenderTreeEdit>(10);
        private readonly ArrayBuilder<RenderTreeFrame> _referenceFrames = new ArrayBuilder<RenderTreeFrame>(10);

        public RenderTreeDiffComputer(Renderer renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        /// <summary>
        /// As well as computing the diff between the two trees, this method also has the side-effect
        /// of instantiating child components on newly-inserted Component frames, and copying the existing
        /// component instances onto retained Component frames. It's particularly convenient to do that
        /// here because we have the right information and are already walking the trees to do the diff.
        /// </summary>
        public void ApplyNewRenderTreeVersion(
            RenderBatchBuilder batchBuilder,
            int componentId,
            ArrayRange<RenderTreeFrame> oldTree,
            ArrayRange<RenderTreeFrame> newTree)
        {
            _entries.Clear();
            _referenceFrames.Clear();
            var siblingIndex = 0;

            var slotId = batchBuilder.ReserveUpdatedComponentSlotId();
            AppendDiffEntriesForRange(batchBuilder, oldTree.Array, 0, oldTree.Count, newTree.Array, 0, newTree.Count, ref siblingIndex);
            batchBuilder.SetUpdatedComponent(
                slotId,
                new RenderTreeDiff(componentId, _entries.ToRange(), _referenceFrames.ToRange()));
        }

        private void AppendDiffEntriesForRange(
            RenderBatchBuilder batchBuilder,
            RenderTreeFrame[] oldTree, int oldStartIndex, int oldEndIndexExcl,
            RenderTreeFrame[] newTree, int newStartIndex, int newEndIndexExcl,
            ref int siblingIndex)
        {
            var hasMoreOld = oldEndIndexExcl > oldStartIndex;
            var hasMoreNew = newEndIndexExcl > newStartIndex;
            var prevOldSeq = -1;
            var prevNewSeq = -1;
            while (hasMoreOld || hasMoreNew)
            {
                var oldSeq = hasMoreOld ? oldTree[oldStartIndex].Sequence : int.MaxValue;
                var newSeq = hasMoreNew ? newTree[newStartIndex].Sequence : int.MaxValue;

                if (oldSeq == newSeq)
                {
                    AppendDiffEntriesForFramesWithSameSequence(batchBuilder, oldTree, oldStartIndex, newTree, newStartIndex, ref siblingIndex);
                    oldStartIndex = NextSiblingIndex(oldTree[oldStartIndex], oldStartIndex);
                    newStartIndex = NextSiblingIndex(newTree[newStartIndex], newStartIndex);
                    hasMoreOld = oldEndIndexExcl > oldStartIndex;
                    hasMoreNew = newEndIndexExcl > newStartIndex;
                    prevOldSeq = oldSeq;
                    prevNewSeq = newSeq;
                }
                else
                {
                    bool treatAsInsert;
                    var oldLoopedBack = oldSeq <= prevOldSeq;
                    var newLoopedBack = newSeq <= prevNewSeq;
                    if (oldLoopedBack == newLoopedBack)
                    {
                        // Both sequences are proceeding through the same loop block, so do a simple
                        // preordered merge join (picking from whichever side brings us closer to being
                        // back in sync)
                        treatAsInsert = newSeq < oldSeq;

                        if (oldLoopedBack)
                        {
                            // If both old and new have now looped back, we must reset their 'looped back'
                            // tracker so we can treat them as proceeding through the same loop block
                            prevOldSeq = prevNewSeq = -1;
                        }
                    }
                    else if (oldLoopedBack)
                    {
                        // Old sequence looped back but new one didn't
                        // The new sequence either has some extra trailing elements in the current loop block
                        // which we should insert, or omits some old trailing loop blocks which we should delete
                        // TODO: Find a way of not recomputing this next flag on every iteration
                        var newLoopsBackLater = false;
                        for (var testIndex = newStartIndex + 1; testIndex < newEndIndexExcl; testIndex++)
                        {
                            if (newTree[testIndex].Sequence < newSeq)
                            {
                                newLoopsBackLater = true;
                                break;
                            }
                        }

                        // If the new sequence loops back later to an earlier point than this,
                        // then we know it's part of the existing loop block (so should be inserted).
                        // If not, then it's unrelated to the previous loop block (so we should treat
                        // the old items as trailing loop blocks to be removed).
                        treatAsInsert = newLoopsBackLater;
                    }
                    else
                    {
                        // New sequence looped back but old one didn't
                        // The old sequence either has some extra trailing elements in the current loop block
                        // which we should delete, or the new sequence has extra trailing loop blocks which we
                        // should insert
                        // TODO: Find a way of not recomputing this next flag on every iteration
                        var oldLoopsBackLater = false;
                        for (var testIndex = oldStartIndex + 1; testIndex < oldEndIndexExcl; testIndex++)
                        {
                            if (oldTree[testIndex].Sequence < oldSeq)
                            {
                                oldLoopsBackLater = true;
                                break;
                            }
                        }

                        // If the old sequence loops back later to an earlier point than this,
                        // then we know it's part of the existing loop block (so should be removed).
                        // If not, then it's unrelated to the previous loop block (so we should treat
                        // the new items as trailing loop blocks to be inserted).
                        treatAsInsert = !oldLoopsBackLater;
                    }

                    if (treatAsInsert)
                    {
                        InsertNewFrame(batchBuilder, newTree, newStartIndex, ref siblingIndex);
                        newStartIndex = NextSiblingIndex(newTree[newStartIndex], newStartIndex);
                        hasMoreNew = newEndIndexExcl > newStartIndex;
                        prevNewSeq = newSeq;
                    }
                    else
                    {
                        RemoveOldFrame(batchBuilder, oldTree, oldStartIndex, siblingIndex);
                        oldStartIndex = NextSiblingIndex(oldTree[oldStartIndex], oldStartIndex);
                        hasMoreOld = oldEndIndexExcl > oldStartIndex;
                        prevOldSeq = oldSeq;
                    }
                }
            }
        }

        private void UpdateRetainedChildComponent(
            RenderBatchBuilder batchBuilder,
            RenderTreeFrame[] oldTree, int oldComponentIndex,
            RenderTreeFrame[] newTree, int newComponentIndex)
        {
            // The algorithm here is the same as in AppendDiffEntriesForRange, except that
            // here we don't optimise for loops - we assume that both sequences are forward-only.
            // That's because this is true for all currently supported scenarios, and it means
            // fewer steps here.

            ref var oldComponentFrame = ref oldTree[oldComponentIndex];
            ref var newComponentFrame = ref newTree[newComponentIndex];
            var componentId = oldComponentFrame.ComponentId;
            var componentInstance = oldComponentFrame.Component;
            var hasSetAnyProperty = false;

            // Preserve the actual componentInstance
            newComponentFrame = newComponentFrame.WithComponentInstance(componentId, componentInstance);

            // Now locate any added/changed/removed properties
            var oldStartIndex = oldComponentIndex + 1;
            var newStartIndex = newComponentIndex + 1;
            var oldEndIndexExcl = oldComponentIndex + oldComponentFrame.ComponentSubtreeLength;
            var newEndIndexExcl = newComponentIndex + newComponentFrame.ComponentSubtreeLength;
            var hasMoreOld = oldEndIndexExcl > oldStartIndex;
            var hasMoreNew = newEndIndexExcl > newStartIndex;
            while (hasMoreOld || hasMoreNew)
            {
                var oldSeq = hasMoreOld ? oldTree[oldStartIndex].Sequence : int.MaxValue;
                var newSeq = hasMoreNew ? newTree[newStartIndex].Sequence : int.MaxValue;

                if (oldSeq == newSeq)
                {
                    ref var oldFrame = ref oldTree[oldStartIndex];
                    ref var newFrame = ref newTree[newStartIndex];
                    var oldName = oldFrame.AttributeName;
                    var newName = newFrame.AttributeName;
                    var newPropertyValue = newFrame.AttributeValue;
                    if (string.Equals(oldName, newName, StringComparison.Ordinal))
                    {
                        // Using Equals to account for string comparisons, nulls, etc.
                        var oldPropertyValue = oldFrame.AttributeValue;
                        if (!Equals(oldPropertyValue, newPropertyValue))
                        {
                            SetChildComponentProperty(componentInstance, newName, newPropertyValue);
                            hasSetAnyProperty = true;
                        }
                    }
                    else
                    {
                        // Since this code path is never reachable for Razor components (because you
                        // can't have two different attribute names from the same source sequence), we
                        // could consider removing the 'name equality' check entirely for perf
                        SetChildComponentProperty(componentInstance, newName, newPropertyValue);
                        RemoveChildComponentProperty(componentInstance, oldName);
                        hasSetAnyProperty = true;
                    }

                    oldStartIndex++;
                    newStartIndex++;
                    hasMoreOld = oldEndIndexExcl > oldStartIndex;
                    hasMoreNew = newEndIndexExcl > newStartIndex;
                }
                else
                {
                    // Both sequences are proceeding through the same loop block, so do a simple
                    // preordered merge join (picking from whichever side brings us closer to being
                    // back in sync)
                    var treatAsInsert = newSeq < oldSeq;

                    if (treatAsInsert)
                    {
                        ref var newFrame = ref newTree[newStartIndex];
                        SetChildComponentProperty(componentInstance, newFrame.AttributeName, newFrame.AttributeValue);
                        hasSetAnyProperty = true;
                        newStartIndex++;
                        hasMoreNew = newEndIndexExcl > newStartIndex;
                    }
                    else
                    {
                        ref var oldFrame = ref oldTree[oldStartIndex];
                        RemoveChildComponentProperty(componentInstance, oldFrame.AttributeName);
                        hasSetAnyProperty = true;
                        oldStartIndex++;
                        hasMoreOld = oldEndIndexExcl > oldStartIndex;
                    }
                }
            }

            if (hasSetAnyProperty)
            {
                TriggerChildComponentRender(batchBuilder, newComponentFrame);
            }
        }

        private static void RemoveChildComponentProperty(IComponent component, string componentPropertyName)
        {
            var propertyInfo = GetChildComponentPropertyInfo(component.GetType(), componentPropertyName);
            var defaultValue = propertyInfo.PropertyType.IsValueType
                ? Activator.CreateInstance(propertyInfo.PropertyType)
                : null;
            SetChildComponentProperty(component, componentPropertyName, defaultValue);
        }

        private static void SetChildComponentProperty(IComponent component, string componentPropertyName, object newPropertyValue)
        {
            var propertyInfo = GetChildComponentPropertyInfo(component.GetType(), componentPropertyName);
            try
            {
                propertyInfo.SetValue(component, newPropertyValue);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Unable to set property '{componentPropertyName}' on component of " +
                    $"type '{component.GetType().FullName}'. The error was: {ex.Message}", ex);
            }
        }

        private static PropertyInfo GetChildComponentPropertyInfo(Type componentType, string componentPropertyName)
        {
            var property = componentType.GetProperty(componentPropertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Component of type '{componentType.FullName}' does not have a property " +
                    $"matching the name '{componentPropertyName}'.");
            }

            return property;
        }

        private static int NextSiblingIndex(RenderTreeFrame frame, int frameIndex)
        {
            var subtreeLength = frame.ElementSubtreeLength;
            var distanceToNextSibling = subtreeLength == 0
                ? 1                 // For frames that don't have a subtree length set, such as text frames
                : subtreeLength;    // For element or component frames
            return frameIndex + distanceToNextSibling;
        }

        private void AppendDiffEntriesForFramesWithSameSequence(
            RenderBatchBuilder batchBuilder,
            RenderTreeFrame[] oldTree, int oldFrameIndex,
            RenderTreeFrame[] newTree, int newFrameIndex,
            ref int siblingIndex)
        {
            ref var oldFrame = ref oldTree[oldFrameIndex];
            ref var newFrame = ref newTree[newFrameIndex];

            // We can assume that the old and new frames are of the same type, because they correspond
            // to the same sequence number (and if not, the behaviour is undefined).
            // TODO: Consider supporting dissimilar types at same sequence for custom IComponent implementations.
            //       It should only be a matter of calling RemoveOldFrame+InsertNewFrame
            switch (newTree[newFrameIndex].FrameType)
            {
                case RenderTreeFrameType.Text:
                    {
                        var oldText = oldFrame.TextContent;
                        var newText = newFrame.TextContent;
                        if (!string.Equals(oldText, newText, StringComparison.Ordinal))
                        {
                            var referenceFrameIndex = _referenceFrames.Append(newFrame);
                            _entries.Append(RenderTreeEdit.UpdateText(siblingIndex, referenceFrameIndex));
                        }
                        siblingIndex++;
                        break;
                    }

                case RenderTreeFrameType.Element:
                    {
                        var oldElementName = oldFrame.ElementName;
                        var newElementName = newFrame.ElementName;
                        if (string.Equals(oldElementName, newElementName, StringComparison.Ordinal))
                        {
                            var oldFrameAttributesEndIndexExcl = GetAttributesEndIndexExclusive(oldTree, oldFrameIndex);
                            var newFrameAttributesEndIndexExcl = GetAttributesEndIndexExclusive(newTree, newFrameIndex);

                            // Diff the attributes
                            AppendDiffEntriesForRange(
                                batchBuilder,
                                oldTree, oldFrameIndex + 1, oldFrameAttributesEndIndexExcl,
                                newTree, newFrameIndex + 1, newFrameAttributesEndIndexExcl,
                                ref siblingIndex);

                            // Diff the children
                            var oldFrameChildrenEndIndexExcl = oldFrameIndex + oldFrame.ElementSubtreeLength;
                            var newFrameChildrenEndIndexExcl = newFrameIndex + newFrame.ElementSubtreeLength;
                            var hasChildrenToProcess =
                                oldFrameChildrenEndIndexExcl > oldFrameAttributesEndIndexExcl ||
                                newFrameChildrenEndIndexExcl > newFrameAttributesEndIndexExcl;
                            if (hasChildrenToProcess)
                            {
                                _entries.Append(RenderTreeEdit.StepIn(siblingIndex));
                                var childSiblingIndex = 0;
                                AppendDiffEntriesForRange(
                                    batchBuilder,
                                    oldTree, oldFrameAttributesEndIndexExcl, oldFrameChildrenEndIndexExcl,
                                    newTree, newFrameAttributesEndIndexExcl, newFrameChildrenEndIndexExcl,
                                    ref childSiblingIndex);
                                AppendStepOut();
                                siblingIndex++;
                            }
                            else
                            {
                                siblingIndex++;
                            }
                        }
                        else
                        {
                            // Elements with different names are treated as completely unrelated
                            RemoveOldFrame(batchBuilder, oldTree, oldFrameIndex, siblingIndex);
                            InsertNewFrame(batchBuilder, newTree, newFrameIndex, ref siblingIndex);
                        }
                        break;
                    }

                case RenderTreeFrameType.Component:
                    {
                        if (oldFrame.ComponentType == newFrame.ComponentType)
                        {
                            UpdateRetainedChildComponent(
                                batchBuilder,
                                oldTree, oldFrameIndex,
                                newTree, newFrameIndex);
                            siblingIndex++;
                        }
                        else
                        {
                            // Child components of different types are treated as completely unrelated
                            RemoveOldFrame(batchBuilder, oldTree, oldFrameIndex, siblingIndex);
                            InsertNewFrame(batchBuilder, newTree, newFrameIndex, ref siblingIndex);
                        }
                        break;
                    }

                case RenderTreeFrameType.Attribute:
                    {
                        var oldName = oldFrame.AttributeName;
                        var newName = newFrame.AttributeName;
                        if (string.Equals(oldName, newName, StringComparison.Ordinal))
                        {
                            // Using Equals to account for string comparisons, nulls, etc.
                            var valueChanged = !Equals(oldFrame.AttributeValue, newFrame.AttributeValue);
                            if (valueChanged)
                            {
                                if (oldFrame.AttributeEventHandlerId > 0)
                                {
                                    batchBuilder.AddDisposedEventHandlerId(oldFrame.AttributeEventHandlerId);
                                }
                                InitializeNewAttributeFrame(ref newFrame);
                                var referenceFrameIndex = _referenceFrames.Append(newFrame);
                                _entries.Append(RenderTreeEdit.SetAttribute(siblingIndex, referenceFrameIndex));
                            }
                            else if (oldFrame.AttributeEventHandlerId > 0)
                            {
                                // Retain the event handler ID
                                newFrame = oldFrame;
                            }
                        }
                        else
                        {
                            // Since this code path is never reachable for Razor components (because you
                            // can't have two different attribute names from the same source sequence), we
                            // could consider removing the 'name equality' check entirely for perf
                            RemoveOldFrame(batchBuilder, oldTree, oldFrameIndex, siblingIndex);
                            InsertNewFrame(batchBuilder, newTree, newFrameIndex, ref siblingIndex);
                        }
                        break;
                    }

                default:
                    throw new NotImplementedException($"Encountered unsupported frame type during diffing: {newTree[newFrameIndex].FrameType}");
            }
        }

        private void InsertNewFrame(RenderBatchBuilder batchBuilder, RenderTreeFrame[] newTree, int newFrameIndex, ref int siblingIndex)
        {
            ref var newFrame = ref newTree[newFrameIndex];
            switch (newFrame.FrameType)
            {
                case RenderTreeFrameType.Attribute:
                    {
                        InitializeNewAttributeFrame(ref newFrame);
                        var referenceFrameIndex = _referenceFrames.Append(newFrame);
                        _entries.Append(RenderTreeEdit.SetAttribute(siblingIndex, referenceFrameIndex));
                        break;
                    }
                case RenderTreeFrameType.Component:
                case RenderTreeFrameType.Element:
                    {
                        InitializeNewSubtree(batchBuilder, newTree, newFrameIndex);
                        var referenceFrameIndex = _referenceFrames.Append(newTree, newFrameIndex, newFrame.ElementSubtreeLength);
                        _entries.Append(RenderTreeEdit.PrependFrame(siblingIndex, referenceFrameIndex));
                        siblingIndex++;
                        break;
                    }
                case RenderTreeFrameType.Text:
                    {
                        var referenceFrameIndex = _referenceFrames.Append(newFrame);
                        _entries.Append(RenderTreeEdit.PrependFrame(siblingIndex, referenceFrameIndex));
                        siblingIndex++;
                        break;
                    }
            }
        }

        private void RemoveOldFrame(RenderBatchBuilder batchBuilder, RenderTreeFrame[] oldTree, int oldFrameIndex, int siblingIndex)
        {
            ref var oldFrame = ref oldTree[oldFrameIndex];
            switch (oldFrame.FrameType)
            {
                case RenderTreeFrameType.Attribute:
                    {
                        _entries.Append(RenderTreeEdit.RemoveAttribute(siblingIndex, oldFrame.AttributeName));
                        if (oldFrame.AttributeEventHandlerId > 0)
                        {
                            batchBuilder.AddDisposedEventHandlerId(oldFrame.AttributeEventHandlerId);
                        }
                        break;
                    }
                case RenderTreeFrameType.Component:
                case RenderTreeFrameType.Element:
                    {
                        var endIndexExcl = oldFrameIndex + oldFrame.ElementSubtreeLength;
                        DisposeFramesInRange(batchBuilder, oldTree, oldFrameIndex, endIndexExcl);
                        _entries.Append(RenderTreeEdit.RemoveFrame(siblingIndex));
                        break;
                    }
                case RenderTreeFrameType.Text:
                    {
                        _entries.Append(RenderTreeEdit.RemoveFrame(siblingIndex));
                        break;
                    }
            }
        }

        private static int GetAttributesEndIndexExclusive(RenderTreeFrame[] tree, int rootIndex)
        {
            var descendantsEndIndexExcl = rootIndex + tree[rootIndex].ElementSubtreeLength;
            var index = rootIndex + 1;
            for (; index < descendantsEndIndexExcl; index++)
            {
                if (tree[index].FrameType != RenderTreeFrameType.Attribute)
                {
                    break;
                }
            }

            return index;
        }

        private void AppendStepOut()
        {
            // If the preceding frame is a StepIn, then the StepOut cancels it out
            var previousIndex = _entries.Count - 1;
            if (previousIndex >= 0 && _entries.Buffer[previousIndex].Type == RenderTreeEditType.StepIn)
            {
                _entries.RemoveLast();
            }
            else
            {
                _entries.Append(RenderTreeEdit.StepOut());
            }
        }

        private void InitializeNewSubtree(RenderBatchBuilder batchBuilder, RenderTreeFrame[] frames, int frameIndex)
        {
            var endIndexExcl = frameIndex + frames[frameIndex].ElementSubtreeLength;
            for (var i = frameIndex; i < endIndexExcl; i++)
            {
                ref var frame = ref frames[i];
                switch (frame.FrameType)
                {
                    case RenderTreeFrameType.Component:
                        InitializeNewComponentFrame(batchBuilder, frames, i);
                        break;
                    case RenderTreeFrameType.Attribute:
                        InitializeNewAttributeFrame(ref frame);
                        break;
                }
            }
        }

        private void InitializeNewComponentFrame(RenderBatchBuilder batchBuilder, RenderTreeFrame[] frames, int frameIndex)
        {
            ref var frame = ref frames[frameIndex];

            if (frame.Component != null)
            {
                throw new InvalidOperationException($"Child component already exists during {nameof(InitializeNewComponentFrame)}");
            }

            _renderer.InstantiateChildComponent(ref frame);
            var childComponentInstance = frame.Component;

            // All descendants of a component are its properties
            var componentDescendantsEndIndexExcl = frameIndex + frame.ComponentSubtreeLength;
            for (var attributeFrameIndex = frameIndex + 1; attributeFrameIndex < componentDescendantsEndIndexExcl; attributeFrameIndex++)
            {
                ref var attributeFrame = ref frames[attributeFrameIndex];
                SetChildComponentProperty(
                    childComponentInstance,
                    attributeFrame.AttributeName,
                    attributeFrame.AttributeValue);
            }

            TriggerChildComponentRender(batchBuilder, frame);
        }

        private void InitializeNewAttributeFrame(ref RenderTreeFrame newFrame)
        {
            if (newFrame.AttributeValue is UIEventHandler)
            {
                _renderer.AssignEventHandlerId(ref newFrame);
            }
        }

        private void TriggerChildComponentRender(RenderBatchBuilder batchBuilder, in RenderTreeFrame frame)
        {
            if (frame.Component is IHandlePropertiesChanged notifyableComponent)
            {
                // TODO: Ensure any exceptions thrown here are handled equivalently to
                // unhandled exceptions during rendering.
                notifyableComponent.OnPropertiesChanged();
            }

            // TODO: Consider moving the responsibility for triggering re-rendering
            // into the OnPropertiesChanged handler (if implemented) so that components
            // can control whether any given set of property changes cause re-rendering.
            // Not doing so yet because it's unclear that the usage patterns would be
            // good to use.
            _renderer.RenderInExistingBatch(batchBuilder, frame.ComponentId);
        }

        internal void DisposeFrames(RenderBatchBuilder batchBuilder, ArrayRange<RenderTreeFrame> frames)
            => DisposeFramesInRange(batchBuilder, frames.Array, 0, frames.Count);

        private void DisposeFramesInRange(RenderBatchBuilder batchBuilder, RenderTreeFrame[] frames, int startIndex, int endIndexExcl)
        {
            for (var i = startIndex; i < endIndexExcl; i++)
            {
                ref var frame = ref frames[i];
                if (frame.FrameType == RenderTreeFrameType.Component && frame.Component != null)
                {
                    _renderer.DisposeInExistingBatch(batchBuilder, frame.ComponentId);
                }
                else if (frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId > 0)
                {
                    batchBuilder.AddDisposedEventHandlerId(frame.AttributeEventHandlerId);
                }
            }
        }
    }
}
