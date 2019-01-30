// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.RenderTree
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

            var diffContext = new DiffContext(renderer, batchBuilder, componentId, oldTree.Array, newTree.Array);
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

        // Handles the diff for attribute nodes only - this invariant is enforced by the caller.
        //
        // The diff for attributes is different because we allow attributes to appear in any order.
        // Put another way, the attributes list of an element or  component is *conceptually* 
        // unordered. This is a case where we can produce a more minimal diff by avoiding 
        // non-meaningful reorderings of attributes.
        private static void AppendAttributeDiffEntriesForRange(
            ref DiffContext diffContext,
            int oldStartIndex, int oldEndIndexExcl,
            int newStartIndex, int newEndIndexExcl)
        {
            // The overhead of the dictionary used by AppendAttributeDiffEntriesForRangeSlow is
            // significant, so we want to try and do a merge-join if possible, but fall back to
            // a hash-join if not. We'll do a merge join until we hit a case we can't handle and
            // then fall back to the slow path.
            //
            // Also since duplicate attributes are not legal, we don't need to care about loops or
            // the more complicated scenarios handled by AppendDiffEntriesForRange.
            //
            // We also assume that we won't see an attribute occur with different sequence numbers
            // in the old and new sequences. It will be handled correct, but will generate a suboptimal
            // diff.
            var hasMoreOld = oldEndIndexExcl > oldStartIndex;
            var hasMoreNew = newEndIndexExcl > newStartIndex;
            var oldTree = diffContext.OldTree;
            var newTree = diffContext.NewTree;

            while (hasMoreOld || hasMoreNew)
            {
                var oldSeq = hasMoreOld ? oldTree[oldStartIndex].Sequence : int.MaxValue;
                var newSeq = hasMoreNew ? newTree[newStartIndex].Sequence : int.MaxValue;
                var oldAttributeName = oldTree[oldStartIndex].AttributeName;
                var newAttributeName = newTree[newStartIndex].AttributeName;

                if (oldSeq == newSeq &&
                    string.Equals(oldAttributeName, newAttributeName, StringComparison.Ordinal))
                {
                    // These two attributes have the same sequence and name. Keep merging.
                    AppendDiffEntriesForAttributeFrame(ref diffContext, oldStartIndex, newStartIndex);

                    oldStartIndex++;
                    newStartIndex++;
                    hasMoreOld = oldEndIndexExcl > oldStartIndex;
                    hasMoreNew = newEndIndexExcl > newStartIndex;
                }
                else if (oldSeq < newSeq)
                {
                    // An attribute was removed compared to the old sequence.
                    RemoveOldFrame(ref diffContext, oldStartIndex);

                    oldStartIndex = NextSiblingIndex(oldTree[oldStartIndex], oldStartIndex);
                    hasMoreOld = oldEndIndexExcl > oldStartIndex;
                }
                else if (oldSeq > newSeq)
                {
                    // An attribute was added compared to the new sequence.
                    InsertNewFrame(ref diffContext, newStartIndex);

                    newStartIndex = NextSiblingIndex(newTree[newStartIndex], newStartIndex);
                    hasMoreNew = newEndIndexExcl > newStartIndex;
                }
                else
                {
                    // These two attributes have the same sequence and different names. This is
                    // a failure case for merge-join, fall back to the slow path.
                    AppendAttributeDiffEntriesForRangeSlow(
                        ref diffContext,
                        oldStartIndex, oldEndIndexExcl,
                        newStartIndex, newEndIndexExcl);
                    return;
                }
            }
        }

        private static void AppendAttributeDiffEntriesForRangeSlow(
            ref DiffContext diffContext,
            int oldStartIndex, int oldEndIndexExcl,
            int newStartIndex, int newEndIndexExcl)
        {
            var oldTree = diffContext.OldTree;
            var newTree = diffContext.NewTree;

            // Slow version of AppendAttributeDiffEntriesForRange that uses a dictionary.
            // Algorithm:
            //
            // 1. iterate through the 'new' tree and add all attributes to the attributes set
            // 2. iterate through the 'old' tree, removing matching attributes from set, and diffing
            // 3. iterate through the remaining attributes in the set and add them
            for (var i = newStartIndex; i < newEndIndexExcl; i++)
            {
                diffContext.AttributeDiffSet[newTree[i].AttributeName] = i;
            }

            for (var i = oldStartIndex; i < oldEndIndexExcl; i++)
            {
                var oldName = oldTree[i].AttributeName;
                if (diffContext.AttributeDiffSet.TryGetValue(oldName, out var matchIndex))
                {
                    // Has a match in the new tree, look for a diff
                    AppendDiffEntriesForAttributeFrame(ref diffContext, i, matchIndex);
                    diffContext.AttributeDiffSet.Remove(oldName);
                }
                else
                {
                    // No match in the new tree, remove old attribute
                    RemoveOldFrame(ref diffContext, i);
                }
            }

            foreach (var kvp in diffContext.AttributeDiffSet)
            {
                // No match in the old tree
                InsertNewFrame(ref diffContext, kvp.Value);
            }

            // We should have processed any additions at this point. Reset for the next batch.
            diffContext.AttributeDiffSet.Clear();
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
            var componentState = oldComponentFrame.ComponentState;

            // Preserve the actual componentInstance
            newComponentFrame = newComponentFrame.WithComponent(componentState);

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
                componentState.SetDirectParameters(newParameters);
            }
        }

        private static int NextSiblingIndex(in RenderTreeFrame frame, int frameIndex)
        {
            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Component:
                    return frameIndex + frame.ComponentSubtreeLength;
                case RenderTreeFrameType.Element:
                    return frameIndex + frame.ElementSubtreeLength;
                case RenderTreeFrameType.Region:
                    return frameIndex + frame.RegionSubtreeLength;
                default:
                    return frameIndex + 1;
            }
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

                case RenderTreeFrameType.Markup:
                    {
                        var oldMarkup = oldFrame.MarkupContent;
                        var newMarkup = newFrame.MarkupContent;
                        if (!string.Equals(oldMarkup, newMarkup, StringComparison.Ordinal))
                        {
                            var referenceFrameIndex = diffContext.ReferenceFrames.Append(newFrame);
                            diffContext.Edits.Append(RenderTreeEdit.UpdateMarkup(diffContext.SiblingIndex, referenceFrameIndex));
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
                            AppendAttributeDiffEntriesForRange(
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

                case RenderTreeFrameType.ElementReferenceCapture:
                    {
                        // We could preserve the ElementReferenceCaptureId from the old frame to the new frame,
                        // and even call newFrame.ElementReferenceCaptureAction(id) each time in case it wants
                        // to do something different with the ID. However there's no known use case for
                        // that, so presently the rule is that for any given element, the reference
                        // capture action is only invoked once.
                        break;
                    }

                    // We don't handle attributes here, they have their own diff logic.
                    // See AppendDiffEntriesForAttributeFrame
                default:
                    throw new NotImplementedException($"Encountered unsupported frame type during diffing: {newTree[newFrameIndex].FrameType}");
            }
        }

        // This should only be called for attributes that have the same name. This is an
        // invariant maintained by the callers.
        private static void AppendDiffEntriesForAttributeFrame(
            ref DiffContext diffContext,
            int oldFrameIndex,
            int newFrameIndex)
        {
            var oldTree = diffContext.OldTree;
            var newTree = diffContext.NewTree;
            ref var oldFrame = ref oldTree[oldFrameIndex];
            ref var newFrame = ref newTree[newFrameIndex];

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
                // Retain the event handler ID by copying the old frame over the new frame.
                // this will prevent us from needing to dispose the old event handler
                // since it was unchanged.
                newFrame = oldFrame;
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
                case RenderTreeFrameType.Markup:
                    {
                        var referenceFrameIndex = diffContext.ReferenceFrames.Append(newFrame);
                        diffContext.Edits.Append(RenderTreeEdit.PrependFrame(diffContext.SiblingIndex, referenceFrameIndex));
                        diffContext.SiblingIndex++;
                        break;
                    }
                case RenderTreeFrameType.ElementReferenceCapture:
                    {
                        InitializeNewElementReferenceCaptureFrame(ref diffContext, ref newFrame);
                        break;
                    }
                case RenderTreeFrameType.ComponentReferenceCapture:
                    {
                        InitializeNewComponentReferenceCaptureFrame(ref diffContext, ref newFrame);
                        break;
                    }
                default:
                    throw new NotImplementedException($"Unexpected frame type during {nameof(InsertNewFrame)}: {newFrame.FrameType}");
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
                case RenderTreeFrameType.Markup:
                    {
                        diffContext.Edits.Append(RenderTreeEdit.RemoveFrame(diffContext.SiblingIndex));
                        break;
                    }
                default:
                    throw new NotImplementedException($"Unexpected frame type during {nameof(RemoveOldFrame)}: {oldFrame.FrameType}");
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
                    case RenderTreeFrameType.ElementReferenceCapture:
                        InitializeNewElementReferenceCaptureFrame(ref diffContext, ref frame);
                        break;
                    case RenderTreeFrameType.ComponentReferenceCapture:
                        InitializeNewComponentReferenceCaptureFrame(ref diffContext, ref frame);
                        break;
                }
            }
        }

        private static void InitializeNewComponentFrame(ref DiffContext diffContext, int frameIndex)
        {
            var frames = diffContext.NewTree;
            ref var frame = ref frames[frameIndex];

            if (frame.ComponentState != null)
            {
                throw new InvalidOperationException($"Child component already exists during {nameof(InitializeNewComponentFrame)}");
            }

            var parentComponentId = diffContext.ComponentId;
            diffContext.Renderer.InstantiateChildComponentOnFrame(ref frame, parentComponentId);
            var childComponentState = frame.ComponentState;

            // Set initial parameters
            var initialParameters = new ParameterCollection(frames, frameIndex);
            childComponentState.SetDirectParameters(initialParameters);
        }

        private static void InitializeNewAttributeFrame(ref DiffContext diffContext, ref RenderTreeFrame newFrame)
        {
            // Any attribute with an event handler id will be callable via DOM events
            //
            // We're following a simple heuristic here that's reflected in the ts runtime
            // based on the common usage of attributes for DOM events.
            if (newFrame.AttributeValue is MulticastDelegate &&
                newFrame.AttributeName.Length >= 3 &&
                newFrame.AttributeName.StartsWith("on"))
            {
                diffContext.Renderer.AssignEventHandlerId(ref newFrame);
            }
        }

        private static void InitializeNewElementReferenceCaptureFrame(ref DiffContext diffContext, ref RenderTreeFrame newFrame)
        {
            var newElementRef = ElementRef.CreateWithUniqueId();
            newFrame = newFrame.WithElementReferenceCaptureId(newElementRef.Id);
            newFrame.ElementReferenceCaptureAction(newElementRef);
        }

        private static void InitializeNewComponentReferenceCaptureFrame(ref DiffContext diffContext, ref RenderTreeFrame newFrame)
        {
            ref var parentFrame = ref diffContext.NewTree[newFrame.ComponentReferenceCaptureParentFrameIndex];
            if (parentFrame.FrameType != RenderTreeFrameType.Component)
            {
                // Should never happen, but will help with diagnosis if it does
                throw new InvalidOperationException($"{nameof(RenderTreeFrameType.ComponentReferenceCapture)} frame references invalid parent index.");
            }

            var componentInstance = parentFrame.Component;
            if (componentInstance == null)
            {
                // Should never happen, but will help with diagnosis if it does
                throw new InvalidOperationException($"Trying to initialize {nameof(RenderTreeFrameType.ComponentReferenceCapture)} frame before parent component was assigned.");
            }

            newFrame.ComponentReferenceCaptureAction(componentInstance);
        }

        private static void DisposeFramesInRange(RenderBatchBuilder batchBuilder, RenderTreeFrame[] frames, int startIndex, int endIndexExcl)
        {
            for (var i = startIndex; i < endIndexExcl; i++)
            {
                ref var frame = ref frames[i];
                if (frame.FrameType == RenderTreeFrameType.Component && frame.ComponentState != null)
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
            public readonly Dictionary<string, int> AttributeDiffSet;
            public readonly int ComponentId;
            public int SiblingIndex;

            public DiffContext(
                Renderer renderer,
                RenderBatchBuilder batchBuilder,
                int componentId,
                RenderTreeFrame[] oldTree,
                RenderTreeFrame[] newTree)
            {
                Renderer = renderer;
                BatchBuilder = batchBuilder;
                ComponentId = componentId;
                OldTree = oldTree;
                NewTree = newTree;
                Edits = batchBuilder.EditsBuffer;
                ReferenceFrames = batchBuilder.ReferenceFramesBuffer;
                AttributeDiffSet = batchBuilder.AttributeDiffSet;
                SiblingIndex = 0;
            }
        }
    }
}
