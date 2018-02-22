// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;

namespace Microsoft.AspNetCore.Blazor.RenderTree
{
    internal static class RenderTreeDiffBuilder
    {
        public static RenderTreeDiff ComputeDiff(
            Renderer renderer,
            RenderBatchBuilder batchBuilder,
            int componentId,
            ArrayRange<RenderTreeFrame> oldTree,
            ArrayRange<RenderTreeFrame> newTree)
        {
            var editsBuffer = batchBuilder.EditsBuffer;
            var editsBufferStartLength = editsBuffer.Count;

            var diffContext = new DiffContext(renderer, batchBuilder, oldTree.Array, newTree.Array);
            AppendDiffEntriesForRange(ref diffContext, 0, oldTree.Count, 0, newTree.Count);

            var editsSegment = editsBuffer.ToSegment(editsBufferStartLength, editsBuffer.Count);
            return new RenderTreeDiff(componentId, editsSegment);
        }

        public static void DisposeFrames(RenderBatchBuilder batchBuilder, ArrayRange<RenderTreeFrame> frames)
            => DisposeFramesInRange(batchBuilder, frames.Array, 0, frames.Count);

        private static void AppendDiffEntriesForRange(
            ref DiffContext diffContext,
            int oldStartIndex, int oldEndIndexExcl,
            int newStartIndex, int newEndIndexExcl)
        {
            var hasMoreOld = oldEndIndexExcl > oldStartIndex;
            var hasMoreNew = newEndIndexExcl > newStartIndex;
            var prevOldSeq = -1;
            var prevNewSeq = -1;
            var oldTree = diffContext.OldTree;
            var newTree = diffContext.NewTree;
            while (hasMoreOld || hasMoreNew)
            {
                var oldSeq = hasMoreOld ? oldTree[oldStartIndex].Sequence : int.MaxValue;
                var newSeq = hasMoreNew ? newTree[newStartIndex].Sequence : int.MaxValue;

                if (oldSeq == newSeq)
                {
                    AppendDiffEntriesForFramesWithSameSequence(ref diffContext, oldStartIndex, newStartIndex);
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
                        InsertNewFrame(ref diffContext, newStartIndex);
                        newStartIndex = NextSiblingIndex(newTree[newStartIndex], newStartIndex);
                        hasMoreNew = newEndIndexExcl > newStartIndex;
                        prevNewSeq = newSeq;
                    }
                    else
                    {
                        RemoveOldFrame(ref diffContext, oldStartIndex);
                        oldStartIndex = NextSiblingIndex(oldTree[oldStartIndex], oldStartIndex);
                        hasMoreOld = oldEndIndexExcl > oldStartIndex;
                        prevOldSeq = oldSeq;
                    }
                }
            }
        }

        private static void UpdateRetainedChildComponent(
            ref DiffContext diffContext,
            int oldComponentIndex,
            int newComponentIndex)
        {
            var oldTree = diffContext.OldTree;
            var newTree = diffContext.NewTree;
            ref var oldComponentFrame = ref oldTree[oldComponentIndex];
            ref var newComponentFrame = ref newTree[newComponentIndex];
            var componentId = oldComponentFrame.ComponentId;
            var componentInstance = oldComponentFrame.Component;

            // Preserve the actual componentInstance
            newComponentFrame = newComponentFrame.WithComponentInstance(componentId, componentInstance);

            // As an important rendering optimization, we want to skip parameter update
            // notifications if we know for sure they haven't changed/mutated. The
            // "MayHaveChangedSince" logic is conservative, in that it returns true if
            // any parameter is of a type we don't know is immutable. In this case
            // we call SetParameters and it's up to the recipient to implement
            // whatever change-detection logic they want. Currently we only supply the new
            // set of parameters and assume the recipient has enough info to do whatever
            // comparisons it wants with the old values. Later we could choose to pass the
            // old parameter values if we wanted. By default, components always rerender
            // after any SetParameters call, which is safe but now always optimal for perf.
            var oldParameters = new ParameterCollection(oldTree, oldComponentIndex);
            var newParameters = new ParameterCollection(newTree, newComponentIndex);
            if (!newParameters.DefinitelyEquals(oldParameters))
            {
                componentInstance.SetParameters(newParameters);
            }
        }

        private static int NextSiblingIndex(RenderTreeFrame frame, int frameIndex)
        {
            var subtreeLength = frame.ElementSubtreeLength;
            var distanceToNextSibling = subtreeLength == 0
                ? 1                 // For frames that don't have a subtree length set, such as text frames
                : subtreeLength;    // For element or component frames
            return frameIndex + distanceToNextSibling;
        }

        private static void AppendDiffEntriesForFramesWithSameSequence(
            ref DiffContext diffContext,
            int oldFrameIndex,
            int newFrameIndex)
        {
            var oldTree = diffContext.OldTree;
            var newTree = diffContext.NewTree;
            ref var oldFrame = ref oldTree[oldFrameIndex];
            ref var newFrame = ref newTree[newFrameIndex];

            // We can assume that the old and new frames are of the same type, because they correspond
            // to the same sequence number (and if not, the behaviour is undefined).
            // TODO: Consider supporting dissimilar types at same sequence for custom IComponent implementations.
            //       It should only be a matter of calling RemoveOldFrame+InsertNewFrame
            switch (newFrame.FrameType)
            {
                case RenderTreeFrameType.Text:
                    {
                        var oldText = oldFrame.TextContent;
                        var newText = newFrame.TextContent;
                        if (!string.Equals(oldText, newText, StringComparison.Ordinal))
                        {
                            var referenceFrameIndex = diffContext.ReferenceFrames.Append(newFrame);
                            diffContext.Edits.Append(RenderTreeEdit.UpdateText(diffContext.SiblingIndex, referenceFrameIndex));
                        }
                        diffContext.SiblingIndex++;
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
                                ref diffContext,
                                oldFrameIndex + 1, oldFrameAttributesEndIndexExcl,
                                newFrameIndex + 1, newFrameAttributesEndIndexExcl);

                            // Diff the children
                            var oldFrameChildrenEndIndexExcl = oldFrameIndex + oldFrame.ElementSubtreeLength;
                            var newFrameChildrenEndIndexExcl = newFrameIndex + newFrame.ElementSubtreeLength;
                            var hasChildrenToProcess =
                                oldFrameChildrenEndIndexExcl > oldFrameAttributesEndIndexExcl ||
                                newFrameChildrenEndIndexExcl > newFrameAttributesEndIndexExcl;
                            if (hasChildrenToProcess)
                            {
                                diffContext.Edits.Append(RenderTreeEdit.StepIn(diffContext.SiblingIndex));
                                var prevSiblingIndex = diffContext.SiblingIndex;
                                diffContext.SiblingIndex = 0;
                                AppendDiffEntriesForRange(
                                    ref diffContext,
                                    oldFrameAttributesEndIndexExcl, oldFrameChildrenEndIndexExcl,
                                    newFrameAttributesEndIndexExcl, newFrameChildrenEndIndexExcl);
                                AppendStepOut(ref diffContext);
                                diffContext.SiblingIndex = prevSiblingIndex + 1;
                            }
                            else
                            {
                                diffContext.SiblingIndex++;
                            }
                        }
                        else
                        {
                            // Elements with different names are treated as completely unrelated
                            RemoveOldFrame(ref diffContext, oldFrameIndex);
                            InsertNewFrame(ref diffContext, newFrameIndex);
                        }
                        break;
                    }

                case RenderTreeFrameType.Region:
                    {
                        AppendDiffEntriesForRange(
                            ref diffContext,
                            oldFrameIndex + 1, oldFrameIndex + oldFrame.RegionSubtreeLength,
                            newFrameIndex + 1, newFrameIndex + newFrame.RegionSubtreeLength);
                        break;
                    }

                case RenderTreeFrameType.Component:
                    {
                        if (oldFrame.ComponentType == newFrame.ComponentType)
                        {
                            UpdateRetainedChildComponent(
                                ref diffContext,
                                oldFrameIndex,
                                newFrameIndex);
                            diffContext.SiblingIndex++;
                        }
                        else
                        {
                            // Child components of different types are treated as completely unrelated
                            RemoveOldFrame(ref diffContext, oldFrameIndex);
                            InsertNewFrame(ref diffContext, newFrameIndex);
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
                                    diffContext.BatchBuilder.DisposedEventHandlerIds.Append(oldFrame.AttributeEventHandlerId);
                                }
                                InitializeNewAttributeFrame(ref diffContext, ref newFrame);
                                var referenceFrameIndex = diffContext.ReferenceFrames.Append(newFrame);
                                diffContext.Edits.Append(RenderTreeEdit.SetAttribute(diffContext.SiblingIndex, referenceFrameIndex));
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
                            RemoveOldFrame(ref diffContext, oldFrameIndex);
                            InsertNewFrame(ref diffContext, newFrameIndex);
                        }
                        break;
                    }

                default:
                    throw new NotImplementedException($"Encountered unsupported frame type during diffing: {newTree[newFrameIndex].FrameType}");
            }
        }

        private static void InsertNewFrame(ref DiffContext diffContext, int newFrameIndex)
        {
            var newTree = diffContext.NewTree;
            ref var newFrame = ref newTree[newFrameIndex];
            switch (newFrame.FrameType)
            {
                case RenderTreeFrameType.Attribute:
                    {
                        InitializeNewAttributeFrame(ref diffContext, ref newFrame);
                        var referenceFrameIndex = diffContext.ReferenceFrames.Append(newFrame);
                        diffContext.Edits.Append(RenderTreeEdit.SetAttribute(diffContext.SiblingIndex, referenceFrameIndex));
                        break;
                    }
                case RenderTreeFrameType.Component:
                case RenderTreeFrameType.Element:
                    {
                        InitializeNewSubtree(ref diffContext, newFrameIndex);
                        var referenceFrameIndex = diffContext.ReferenceFrames.Append(newTree, newFrameIndex, newFrame.ElementSubtreeLength);
                        diffContext.Edits.Append(RenderTreeEdit.PrependFrame(diffContext.SiblingIndex, referenceFrameIndex));
                        diffContext.SiblingIndex++;
                        break;
                    }
                case RenderTreeFrameType.Region:
                    {
                        var regionChildFrameIndex = newFrameIndex + 1;
                        var regionChildFrameEndIndexExcl = newFrameIndex + newFrame.RegionSubtreeLength;
                        while (regionChildFrameIndex < regionChildFrameEndIndexExcl)
                        {
                            InsertNewFrame(ref diffContext, regionChildFrameIndex);
                            regionChildFrameIndex = NextSiblingIndex(newTree[regionChildFrameIndex], regionChildFrameIndex);
                        }
                        break;
                    }
                case RenderTreeFrameType.Text:
                    {
                        var referenceFrameIndex = diffContext.ReferenceFrames.Append(newFrame);
                        diffContext.Edits.Append(RenderTreeEdit.PrependFrame(diffContext.SiblingIndex, referenceFrameIndex));
                        diffContext.SiblingIndex++;
                        break;
                    }
            }
        }

        private static void RemoveOldFrame(ref DiffContext diffContext, int oldFrameIndex)
        {
            var oldTree = diffContext.OldTree;
            ref var oldFrame = ref oldTree[oldFrameIndex];
            switch (oldFrame.FrameType)
            {
                case RenderTreeFrameType.Attribute:
                    {
                        diffContext.Edits.Append(RenderTreeEdit.RemoveAttribute(diffContext.SiblingIndex, oldFrame.AttributeName));
                        if (oldFrame.AttributeEventHandlerId > 0)
                        {
                            diffContext.BatchBuilder.DisposedEventHandlerIds.Append(oldFrame.AttributeEventHandlerId);
                        }
                        break;
                    }
                case RenderTreeFrameType.Component:
                case RenderTreeFrameType.Element:
                    {
                        var endIndexExcl = oldFrameIndex + oldFrame.ElementSubtreeLength;
                        DisposeFramesInRange(diffContext.BatchBuilder, oldTree, oldFrameIndex, endIndexExcl);
                        diffContext.Edits.Append(RenderTreeEdit.RemoveFrame(diffContext.SiblingIndex));
                        break;
                    }
                case RenderTreeFrameType.Region:
                    {
                        var regionChildFrameIndex = oldFrameIndex + 1;
                        var regionChildFrameEndIndexExcl = oldFrameIndex + oldFrame.RegionSubtreeLength;
                        while (regionChildFrameIndex < regionChildFrameEndIndexExcl)
                        {
                            RemoveOldFrame(ref diffContext, regionChildFrameIndex);
                            regionChildFrameIndex = NextSiblingIndex(oldTree[regionChildFrameIndex], regionChildFrameIndex);
                        }
                        break;
                    }
                case RenderTreeFrameType.Text:
                    {
                        diffContext.Edits.Append(RenderTreeEdit.RemoveFrame(diffContext.SiblingIndex));
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

        private static void AppendStepOut(ref DiffContext diffContext)
        {
            // If the preceding frame is a StepIn, then the StepOut cancels it out
            var previousIndex = diffContext.Edits.Count - 1;
            if (previousIndex >= 0 && diffContext.Edits.Buffer[previousIndex].Type == RenderTreeEditType.StepIn)
            {
                diffContext.Edits.RemoveLast();
            }
            else
            {
                diffContext.Edits.Append(RenderTreeEdit.StepOut());
            }
        }

        private static void InitializeNewSubtree(ref DiffContext diffContext, int frameIndex)
        {
            var frames = diffContext.NewTree;
            var endIndexExcl = frameIndex + frames[frameIndex].ElementSubtreeLength;
            for (var i = frameIndex; i < endIndexExcl; i++)
            {
                ref var frame = ref frames[i];
                switch (frame.FrameType)
                {
                    case RenderTreeFrameType.Component:
                        InitializeNewComponentFrame(ref diffContext, i);
                        break;
                    case RenderTreeFrameType.Attribute:
                        InitializeNewAttributeFrame(ref diffContext, ref frame);
                        break;
                }
            }
        }

        private static void InitializeNewComponentFrame(ref DiffContext diffContext, int frameIndex)
        {
            var frames = diffContext.NewTree;
            ref var frame = ref frames[frameIndex];

            if (frame.Component != null)
            {
                throw new InvalidOperationException($"Child component already exists during {nameof(InitializeNewComponentFrame)}");
            }

            diffContext.Renderer.InstantiateChildComponentOnFrame(ref frame);
            var childComponentInstance = frame.Component;

            // Set initial parameters
            var initialParameters = new ParameterCollection(frames, frameIndex);
            childComponentInstance.SetParameters(initialParameters);
        }

        private static void InitializeNewAttributeFrame(ref DiffContext diffContext, ref RenderTreeFrame newFrame)
        {
            if (newFrame.AttributeValue is UIEventHandler)
            {
                diffContext.Renderer.AssignEventHandlerId(ref newFrame);
            }
        }

        private static void DisposeFramesInRange(RenderBatchBuilder batchBuilder, RenderTreeFrame[] frames, int startIndex, int endIndexExcl)
        {
            for (var i = startIndex; i < endIndexExcl; i++)
            {
                ref var frame = ref frames[i];
                if (frame.FrameType == RenderTreeFrameType.Component && frame.Component != null)
                {
                    batchBuilder.ComponentDisposalQueue.Enqueue(frame.ComponentId);
                }
                else if (frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerId > 0)
                {
                    batchBuilder.DisposedEventHandlerIds.Append(frame.AttributeEventHandlerId);
                }
            }
        }

        /// <summary>
        /// Exists only so that the various methods in this class can call each other without
        /// constantly building up long lists of parameters. Is private to this class, so the
        /// fact that it's a mutable struct is manageable.
        /// 
        /// Always pass by ref to avoid copying, and because the 'SiblingIndex' is mutable.
        /// </summary>
        private struct DiffContext
        {
            public readonly Renderer Renderer;
            public readonly RenderBatchBuilder BatchBuilder;
            public readonly RenderTreeFrame[] OldTree;
            public readonly RenderTreeFrame[] NewTree;
            public readonly ArrayBuilder<RenderTreeEdit> Edits;
            public readonly ArrayBuilder<RenderTreeFrame> ReferenceFrames;
            public int SiblingIndex;

            public DiffContext(
                Renderer renderer,
                RenderBatchBuilder batchBuilder,
                RenderTreeFrame[] oldTree,
                RenderTreeFrame[] newTree)
            {
                Renderer = renderer;
                BatchBuilder = batchBuilder;
                OldTree = oldTree;
                NewTree = newTree;
                Edits = batchBuilder.EditsBuffer;
                ReferenceFrames = batchBuilder.ReferenceFramesBuffer;
                SiblingIndex = 0;
            }
        }
    }
}
