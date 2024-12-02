// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable warnings

using System.Diagnostics;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.RenderTree;

internal static class RenderTreeDiffBuilder
{
    enum DiffAction { Match, Insert, Delete }

    // We use int.MinValue to signal this special case because (1) it would never be used by
    // the Razor compiler or by accident in developer code, and (2) we know it will always
    // hit the "old < new" code path during diffing so we only have to check for it in one place.
    public const int SystemAddedAttributeSequenceNumber = int.MinValue;

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
        var result = new RenderTreeDiff(componentId, editsSegment);

        return result;
    }

    public static void DisposeFrames(RenderBatchBuilder batchBuilder, int componentId, ArrayRange<RenderTreeFrame> frames)
        => DisposeFramesInRange(batchBuilder, componentId, frames.Array, 0, frames.Count);

    private static void AppendDiffEntriesForRange(
        ref DiffContext diffContext,
        int oldStartIndex, int oldEndIndexExcl,
        int newStartIndex, int newEndIndexExcl)
    {
        // This is deliberately a very large method. Parts of it could be factored out
        // into other private methods, but doing so comes at a consequential perf cost,
        // because it involves so much parameter passing. You can think of the code here
        // as being several private methods (delimited by #region) pre-inlined.
        //
        // A naive "extract methods"-type refactoring will worsen perf by about 10%. So,
        // if you plan to refactor this, be sure to benchmark the old and new versions
        // on Mono WebAssembly.

        var origOldStartIndex = oldStartIndex;
        var origNewStartIndex = newStartIndex;
        var hasMoreOld = oldEndIndexExcl > oldStartIndex;
        var hasMoreNew = newEndIndexExcl > newStartIndex;
        var prevOldSeq = -1;
        var prevNewSeq = -1;
        var oldTree = diffContext.OldTree;
        var newTree = diffContext.NewTree;
        var matchWithNewTreeIndex = -1; // Only used when action == DiffAction.Match
        Dictionary<object, KeyedItemInfo> keyedItemInfos = null;

        try
        {
            while (hasMoreOld || hasMoreNew)
            {
                DiffAction action;

                #region "Read keys and sequence numbers"
                int oldSeq, newSeq;
                object oldKey, newKey;
                if (hasMoreOld)
                {
                    ref var oldFrame = ref oldTree[oldStartIndex];
                    oldSeq = oldFrame.SequenceField;
                    oldKey = KeyValue(ref oldFrame);
                }
                else
                {
                    oldSeq = int.MaxValue;
                    oldKey = null;
                }

                if (hasMoreNew)
                {
                    ref var newFrame = ref newTree[newStartIndex];
                    newSeq = newFrame.SequenceField;
                    newKey = KeyValue(ref newFrame);
                }
                else
                {
                    newSeq = int.MaxValue;
                    newKey = null;
                }
                #endregion

                // If there's a key on either side, prefer matching by key not sequence
                if (oldKey != null || newKey != null)
                {
                    #region "Get diff action by matching on key"
                    // Regardless of whether these two keys match, since you are using keys, we want to validate at this point that there are no clashes
                    // so ensure we've built the dictionary that will be used for lookups if any don't match
                    keyedItemInfos ??= BuildKeyToInfoLookup(diffContext, origOldStartIndex, oldEndIndexExcl, origNewStartIndex, newEndIndexExcl);

                    if (Equals(oldKey, newKey))
                    {
                        // Keys match
                        action = DiffAction.Match;
                        matchWithNewTreeIndex = newStartIndex;
                    }
                    else
                    {
                        // Keys don't match
                        var oldKeyItemInfo = oldKey != null ? keyedItemInfos[oldKey] : new KeyedItemInfo(-1, -1);
                        var newKeyItemInfo = newKey != null ? keyedItemInfos[newKey] : new KeyedItemInfo(-1, -1);
                        var oldKeyIsInNewTree = oldKeyItemInfo.NewIndex >= 0;
                        var newKeyIsInOldTree = newKeyItemInfo.OldIndex >= 0;

                        // If either key is not in the other tree, we can handle it as an insert or a delete
                        // on this iteration. We're only forced to use the move logic that's not the case
                        // (i.e., both keys are in both trees)
                        if (oldKeyIsInNewTree && newKeyIsInOldTree)
                        {
                            // It's a move
                            // Since the recipient of the diff script already has the old frame (the one with oldKey)
                            // at the current siblingIndex, recurse into oldKey and update its descendants in place.
                            // We re-order the frames afterwards.
                            action = DiffAction.Match;
                            matchWithNewTreeIndex = oldKeyItemInfo.NewIndex;

                            // Track the post-edit sibling indices of the moved items
                            // Since diffContext.SiblingIndex only increases, we can be sure the values we
                            // write at this point will remain correct, because there won't be any further
                            // insertions/deletions at smaller sibling indices.
                            keyedItemInfos[oldKey] = oldKeyItemInfo.WithOldSiblingIndex(diffContext.SiblingIndex);
                            keyedItemInfos[newKey] = newKeyItemInfo.WithNewSiblingIndex(diffContext.SiblingIndex);
                        }
                        else if (newKey == null)
                        {
                            // Only the old side is keyed.
                            // - If it was retained, we're inserting the new (unkeyed) side.
                            // - If it wasn't retained, we're deleting the old (keyed) side.
                            action = oldKeyIsInNewTree ? DiffAction.Insert : DiffAction.Delete;
                        }
                        else
                        {
                            // The rationale is complex here because there are a lot of different cases that
                            // merge together into the same rule.
                            // | old is keyed | newKeyIsInOldTree | oldKeyIsInNewTree  | Outcome   |
                            // | ------------ | ----------------- | ------------------ | --------- |
                            // | false        | true              | n/a - it's unkeyed | Delete    |
                            // | false        | false             | n/a - it's unkeyed | Insert    |
                            // | true         | true              | must be false[1]   | Delete    |
                            // | true         | false             | true               | Insert    |
                            // | true         | false             | false              | Insert[2] |
                            // [1] because we already know they were not both retained (checked above)
                            // [2] because neither was retained, so it's both an insert and a delete, and thus
                            //     we can pick either one to handle on this iteration, and the other will be
                            //     found and handled on the next iteration
                            // So all cases can be handled by the following simple criterion, which is pleasingly
                            // symetrically opposite the case for newKey==null.
                            action = newKeyIsInOldTree ? DiffAction.Delete : DiffAction.Insert;
                        }

                        // The above logic doesn't explicitly talk about whether or not we've run out of items on either
                        // side of the comparison. If we do run out of items on either side, the logic should result in
                        // us picking the remaining items from the other side to insert/delete. The following assertion is
                        // just to simplify debugging if future logic changes violate this.
                        Debug.Assert(action switch
                        {
                            DiffAction.Insert => hasMoreNew,
                            DiffAction.Delete => hasMoreOld,
                            _ => true,
                        }, "The chosen diff action is illegal because we've run out of items on the side being inserted/deleted");
                    }
                    #endregion
                }
                else
                {
                    #region "Get diff action by matching on sequence number"
                    // Neither side is keyed, so match by sequence number
                    if (oldSeq == newSeq)
                    {
                        // Sequences match
                        action = DiffAction.Match;
                        matchWithNewTreeIndex = newStartIndex;
                    }
                    else
                    {
                        // Sequences don't match
                        var oldLoopedBack = oldSeq <= prevOldSeq;
                        var newLoopedBack = newSeq <= prevNewSeq;
                        if (oldLoopedBack == newLoopedBack)
                        {
                            // Both sequences are proceeding through the same loop block, so do a simple
                            // preordered merge join (picking from whichever side brings us closer to being
                            // back in sync)
                            action = newSeq < oldSeq ? DiffAction.Insert : DiffAction.Delete;

                            if (oldLoopedBack)
                            {
                                // If both old and new have now looped back, we must reset their 'looped back'
                                // tracker so we can treat them as proceeding through the same loop block
                                prevOldSeq = -1;
                                prevNewSeq = -1;
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
                                if (newTree[testIndex].SequenceField < newSeq)
                                {
                                    newLoopsBackLater = true;
                                    break;
                                }
                            }

                            // If the new sequence loops back later to an earlier point than this,
                            // then we know it's part of the existing loop block (so should be inserted).
                            // If not, then it's unrelated to the previous loop block (so we should treat
                            // the old items as trailing loop blocks to be removed).
                            action = newLoopsBackLater ? DiffAction.Insert : DiffAction.Delete;
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
                                if (oldTree[testIndex].SequenceField < oldSeq)
                                {
                                    oldLoopsBackLater = true;
                                    break;
                                }
                            }

                            // If the old sequence loops back later to an earlier point than this,
                            // then we know it's part of the existing loop block (so should be removed).
                            // If not, then it's unrelated to the previous loop block (so we should treat
                            // the new items as trailing loop blocks to be inserted).
                            action = oldLoopsBackLater ? DiffAction.Delete : DiffAction.Insert;
                        }
                    }
                    #endregion
                }

                #region "Apply diff action"
                switch (action)
                {
                    case DiffAction.Match:
                        AppendDiffEntriesForFramesWithSameSequence(ref diffContext, oldStartIndex, matchWithNewTreeIndex);
                        oldStartIndex = NextSiblingIndex(oldTree[oldStartIndex], oldStartIndex);
                        newStartIndex = NextSiblingIndex(newTree[newStartIndex], newStartIndex);
                        hasMoreOld = oldEndIndexExcl > oldStartIndex;
                        hasMoreNew = newEndIndexExcl > newStartIndex;
                        prevOldSeq = oldSeq;
                        prevNewSeq = newSeq;
                        break;
                    case DiffAction.Insert:
                        InsertNewFrame(ref diffContext, newStartIndex);
                        newStartIndex = NextSiblingIndex(newTree[newStartIndex], newStartIndex);
                        hasMoreNew = newEndIndexExcl > newStartIndex;
                        prevNewSeq = newSeq;
                        break;
                    case DiffAction.Delete:
                        RemoveOldFrame(ref diffContext, oldStartIndex);
                        oldStartIndex = NextSiblingIndex(oldTree[oldStartIndex], oldStartIndex);
                        hasMoreOld = oldEndIndexExcl > oldStartIndex;
                        prevOldSeq = oldSeq;
                        break;
                }
                #endregion
            }

            #region "Write permutations list"
            if (keyedItemInfos != null)
            {
                var hasPermutations = false;
                foreach (var keyValuePair in keyedItemInfos)
                {
                    var value = keyValuePair.Value;
                    if (value.OldSiblingIndex >= 0 && value.NewSiblingIndex >= 0)
                    {
                        // This item moved
                        hasPermutations = true;
                        diffContext.Edits.Append(
                            RenderTreeEdit.PermutationListEntry(value.OldSiblingIndex, value.NewSiblingIndex));
                    }
                }

                if (hasPermutations)
                {
                    // It's much easier for the recipient to handle if we're explicit about
                    // when the list is finished
                    diffContext.Edits.Append(RenderTreeEdit.PermutationListEnd());
                }
            }
            #endregion
        }
        finally
        {
            if (keyedItemInfos != null)
            {
                keyedItemInfos.Clear();
                diffContext.KeyedItemInfoDictionaryPool.Return(keyedItemInfos);
            }
        }
    }

    private static Dictionary<object, KeyedItemInfo> BuildKeyToInfoLookup(DiffContext diffContext, int oldStartIndex, int oldEndIndexExcl, int newStartIndex, int newEndIndexExcl)
    {
        var result = diffContext.KeyedItemInfoDictionaryPool.Get();
        var oldTree = diffContext.OldTree;
        var newTree = diffContext.NewTree;

        while (oldStartIndex < oldEndIndexExcl)
        {
            ref var frame = ref oldTree[oldStartIndex];
            var key = KeyValue(ref frame);
            if (key != null)
            {
                if (result.ContainsKey(key))
                {
                    ThrowExceptionForDuplicateKey(key, frame);
                }

                result[key] = new KeyedItemInfo(oldStartIndex, -1);
            }

            oldStartIndex = NextSiblingIndex(frame, oldStartIndex);
        }

        while (newStartIndex < newEndIndexExcl)
        {
            ref var frame = ref newTree[newStartIndex];
            var key = KeyValue(ref frame);
            if (key != null)
            {
                if (!result.TryGetValue(key, out var existingEntry))
                {
                    result[key] = new KeyedItemInfo(-1, newStartIndex);
                }
                else
                {
                    if (existingEntry.NewIndex >= 0)
                    {
                        ThrowExceptionForDuplicateKey(key, frame);
                    }

                    result[key] = new KeyedItemInfo(existingEntry.OldIndex, newStartIndex);
                }
            }

            newStartIndex = NextSiblingIndex(frame, newStartIndex);
        }

        return result;
    }

    private static void ThrowExceptionForDuplicateKey(object key, in RenderTreeFrame frame)
    {
        switch (frame.FrameTypeField)
        {
            case RenderTreeFrameType.Component:
                throw new InvalidOperationException($"More than one sibling of component '{frame.ComponentTypeField}' has the same key value, '{key}'. Key values must be unique.");

            case RenderTreeFrameType.Element:
                throw new InvalidOperationException($"More than one sibling of element '{frame.ElementNameField}' has the same key value, '{key}'. Key values must be unique.");

            default:
                throw new InvalidOperationException($"More than one sibling has the same key value, '{key}'. Key values must be unique.");
        }
    }

    private static object KeyValue(ref RenderTreeFrame frame)
    {
        switch (frame.FrameTypeField)
        {
            case RenderTreeFrameType.Element:
                return frame.ElementKeyField;
            case RenderTreeFrameType.Component:
                return frame.ComponentKeyField;
            default:
                return null;
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
            var (oldSeq, oldAttributeName) = hasMoreOld
                ? (oldTree[oldStartIndex].SequenceField, oldTree[oldStartIndex].AttributeNameField)
                : (int.MaxValue, null);
            var (newSeq, newAttributeName) = hasMoreNew
                ? (newTree[newStartIndex].SequenceField, newTree[newStartIndex].AttributeNameField)
                : (int.MaxValue, null);

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
                if (oldSeq == SystemAddedAttributeSequenceNumber)
                {
                    // This special sequence number means that we can't rely on the sequence numbers
                    // for matching and are forced to fall back on the dictionary-based join in order
                    // to produce an optimal diff. If we didn't we'd likely produce a diff that removes
                    // and then re-adds the same attribute.
                    // We use the special sequence number to signal it because it adds almost no cost
                    // to check for it only in this one case.
                    AppendAttributeDiffEntriesForRangeSlow(
                        ref diffContext,
                        oldStartIndex, oldEndIndexExcl,
                        newStartIndex, newEndIndexExcl);
                    return;
                }

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
            diffContext.AttributeDiffSet[newTree[i].AttributeNameField] = i;
        }

        for (var i = oldStartIndex; i < oldEndIndexExcl; i++)
        {
            var oldName = oldTree[i].AttributeNameField;
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

    private static int NextSiblingIndex(in RenderTreeFrame frame, int frameIndex)
    {
        switch (frame.FrameTypeField)
        {
            case RenderTreeFrameType.Component:
                return frameIndex + frame.ComponentSubtreeLengthField;
            case RenderTreeFrameType.Element:
                return frameIndex + frame.ElementSubtreeLengthField;
            case RenderTreeFrameType.Region:
                return frameIndex + frame.RegionSubtreeLengthField;
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

        // This can't happen for sequence-matched frames from .razor components, but it can happen if you write your
        // builder logic manually or if two dissimilar frames matched by key. Treat as completely unrelated.
        var newFrameType = newFrame.FrameTypeField;
        if (oldFrame.FrameTypeField != newFrameType)
        {
            InsertNewFrame(ref diffContext, newFrameIndex);
            RemoveOldFrame(ref diffContext, oldFrameIndex);
            return;
        }

        // We can assume that the old and new frames are of the same type, because they correspond
        // to the same sequence number (and if not, the behaviour is undefined).
        // TODO: Consider supporting dissimilar types at same sequence for custom IComponent implementations.
        //       It should only be a matter of calling RemoveOldFrame+InsertNewFrame
        switch (newFrameType)
        {
            case RenderTreeFrameType.Text:
                {
                    var oldText = oldFrame.TextContentField;
                    var newText = newFrame.TextContentField;
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
                    var oldMarkup = oldFrame.MarkupContentField;
                    var newMarkup = newFrame.MarkupContentField;
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
                    var oldElementName = oldFrame.ElementNameField;
                    var newElementName = newFrame.ElementNameField;
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
                        var oldFrameChildrenEndIndexExcl = oldFrameIndex + oldFrame.ElementSubtreeLengthField;
                        var newFrameChildrenEndIndexExcl = newFrameIndex + newFrame.ElementSubtreeLengthField;
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
                        oldFrameIndex + 1, oldFrameIndex + oldFrame.RegionSubtreeLengthField,
                        newFrameIndex + 1, newFrameIndex + newFrame.RegionSubtreeLengthField);
                    break;
                }

            case RenderTreeFrameType.Component:
                {
                    if (oldFrame.ComponentTypeField == newFrame.ComponentTypeField)
                    {
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

                        // When performing hot reload, we want to force all components to re-render.
                        // We do this using two mechanisms - we call SetParametersAsync even if the parameters
                        // are unchanged and we ignore ComponentBase.ShouldRender.
                        // Furthermore, when a hot reload edit removes component parameters, the component should be
                        // disposed and reinstantiated. This allows the component's construction logic to correctly
                        // re-initialize the removed parameter properties.

                        var oldParameters = new ParameterView(ParameterViewLifetime.Unbound, oldTree, oldFrameIndex);
                        var newParametersLifetime = new ParameterViewLifetime(diffContext.BatchBuilder);
                        var newParameters = new ParameterView(newParametersLifetime, newTree, newFrameIndex);
                        var isHotReload = HotReloadManager.Default.MetadataUpdateSupported && diffContext.Renderer.IsRenderingOnMetadataUpdate;

                        if (isHotReload && newParameters.HasRemovedDirectParameters(oldParameters))
                        {
                            // Components with parameters removed during a hot reload edit should be disposed and reinstantiated
                            RemoveOldFrame(ref diffContext, oldFrameIndex);
                            InsertNewFrame(ref diffContext, newFrameIndex);
                        }
                        else
                        {
                            var componentState = oldFrame.ComponentStateField;

                            // Preserve the actual componentInstance
                            newFrame.ComponentStateField = componentState;
                            newFrame.ComponentIdField = componentState.ComponentId;

                            if (!newParameters.DefinitelyEquals(oldParameters) || isHotReload)
                            {
                                componentState.SetDirectParameters(newParameters);
                            }

                            diffContext.SiblingIndex++;
                        }
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

            case RenderTreeFrameType.NamedEvent:
                {
                    // We don't have a use case for the event types changing, so we don't even check that. We assume for a given sequence number
                    // the event type is always a constant. What can change is the frame index and the assigned name.
                    if (oldFrameIndex != newFrameIndex
                        || !string.Equals(oldFrame.NamedEventAssignedName, newFrame.NamedEventAssignedName, StringComparison.Ordinal))
                    {
                        // We could track the updates as a concept in its own right, but this situation will be uncommon,
                        // so it's enough to treat it as a delete+add
                        diffContext.BatchBuilder.RemoveNamedEvent(diffContext.ComponentId, oldFrameIndex, ref oldFrame);
                        diffContext.BatchBuilder.AddNamedEvent(diffContext.ComponentId, newFrameIndex, ref newFrame);
                    }

                    break;
                }

            // We don't handle attributes here, they have their own diff logic.
            // See AppendDiffEntriesForAttributeFrame
            default:
                throw new NotImplementedException($"Encountered unsupported frame type during diffing: {newTree[newFrameIndex].FrameTypeField}");
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
        var valueChanged = !Equals(oldFrame.AttributeValueField, newFrame.AttributeValueField);
        if (valueChanged)
        {
            InitializeNewAttributeFrame(ref diffContext, ref newFrame);
            var referenceFrameIndex = diffContext.ReferenceFrames.Append(newFrame);
            diffContext.Edits.Append(RenderTreeEdit.SetAttribute(diffContext.SiblingIndex, referenceFrameIndex));

            // If we're replacing an old event handler ID with a new one, register the old one for disposal,
            // plus keep track of the old->new chain until the old one is fully disposed
            if (oldFrame.AttributeEventHandlerIdField > 0)
            {
                diffContext.Renderer.TrackReplacedEventHandlerId(oldFrame.AttributeEventHandlerIdField, newFrame.AttributeEventHandlerIdField);
                diffContext.BatchBuilder.DisposedEventHandlerIds.Append(oldFrame.AttributeEventHandlerIdField);
            }
        }
        else if (oldFrame.AttributeEventHandlerIdField > 0)
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
        switch (newFrame.FrameTypeField)
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
                    var referenceFrameIndex = diffContext.ReferenceFrames.Append(newTree, newFrameIndex, newFrame.ElementSubtreeLengthField);
                    diffContext.Edits.Append(RenderTreeEdit.PrependFrame(diffContext.SiblingIndex, referenceFrameIndex));
                    diffContext.SiblingIndex++;
                    break;
                }
            case RenderTreeFrameType.Region:
                {
                    var regionChildFrameIndex = newFrameIndex + 1;
                    var regionChildFrameEndIndexExcl = newFrameIndex + newFrame.RegionSubtreeLengthField;
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
            case RenderTreeFrameType.NamedEvent:
                {
                    InitializeNewNamedEvent(ref diffContext, newFrameIndex);
                    break;
                }
            default:
                throw new NotImplementedException($"Unexpected frame type during {nameof(InsertNewFrame)}: {newFrame.FrameTypeField}");
        }
    }

    private static void RemoveOldFrame(ref DiffContext diffContext, int oldFrameIndex)
    {
        var oldTree = diffContext.OldTree;
        ref var oldFrame = ref oldTree[oldFrameIndex];
        switch (oldFrame.FrameTypeField)
        {
            case RenderTreeFrameType.Attribute:
                {
                    diffContext.Edits.Append(RenderTreeEdit.RemoveAttribute(diffContext.SiblingIndex, oldFrame.AttributeNameField));
                    if (oldFrame.AttributeEventHandlerIdField > 0)
                    {
                        diffContext.BatchBuilder.DisposedEventHandlerIds.Append(oldFrame.AttributeEventHandlerIdField);
                    }
                    break;
                }
            case RenderTreeFrameType.Component:
            case RenderTreeFrameType.Element:
                {
                    var endIndexExcl = oldFrameIndex + oldFrame.ElementSubtreeLengthField;
                    DisposeFramesInRange(diffContext.BatchBuilder, diffContext.ComponentId, oldTree, oldFrameIndex, endIndexExcl);
                    diffContext.Edits.Append(RenderTreeEdit.RemoveFrame(diffContext.SiblingIndex));
                    break;
                }
            case RenderTreeFrameType.Region:
                {
                    var regionChildFrameIndex = oldFrameIndex + 1;
                    var regionChildFrameEndIndexExcl = oldFrameIndex + oldFrame.RegionSubtreeLengthField;
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
            case RenderTreeFrameType.NamedEvent:
                {
                    diffContext.BatchBuilder.RemoveNamedEvent(diffContext.ComponentId, oldFrameIndex, ref diffContext.OldTree[oldFrameIndex]);
                    break;
                }
            default:
                throw new NotImplementedException($"Unexpected frame type during {nameof(RemoveOldFrame)}: {oldFrame.FrameTypeField}");
        }
    }

    private static int GetAttributesEndIndexExclusive(RenderTreeFrame[] tree, int rootIndex)
    {
        var descendantsEndIndexExcl = rootIndex + tree[rootIndex].ElementSubtreeLengthField;
        var index = rootIndex + 1;
        for (; index < descendantsEndIndexExcl; index++)
        {
            if (tree[index].FrameTypeField != RenderTreeFrameType.Attribute)
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
        var endIndexExcl = frameIndex + frames[frameIndex].ElementSubtreeLengthField;
        for (var i = frameIndex; i < endIndexExcl; i++)
        {
            ref var frame = ref frames[i];
            switch (frame.FrameTypeField)
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
                case RenderTreeFrameType.NamedEvent:
                    InitializeNewNamedEvent(ref diffContext, i);
                    break;
            }
        }
    }

    private static void InitializeNewComponentFrame(ref DiffContext diffContext, int frameIndex)
    {
        var frames = diffContext.NewTree;
        ref var frame = ref frames[frameIndex];
        var parentComponentId = diffContext.ComponentId;
        var childComponentState = diffContext.Renderer.InstantiateChildComponentOnFrame(frames, frameIndex, parentComponentId);

        // Set initial parameters
        var initialParametersLifetime = new ParameterViewLifetime(diffContext.BatchBuilder);
        var initialParameters = new ParameterView(initialParametersLifetime, frames, frameIndex);
        childComponentState.SetDirectParameters(initialParameters);
    }

    private static void InitializeNewAttributeFrame(ref DiffContext diffContext, ref RenderTreeFrame newFrame)
    {
        // Any attribute with an event handler id will be callable via DOM events
        //
        // We're following a simple heuristic here that's reflected in the ts runtime
        // based on the common usage of attributes for DOM events.
        if ((newFrame.AttributeValueField is MulticastDelegate || newFrame.AttributeValueField is EventCallback) &&
            newFrame.AttributeNameField.Length >= 3 &&
            newFrame.AttributeNameField.StartsWith("on", StringComparison.Ordinal))
        {
            diffContext.Renderer.AssignEventHandlerId(diffContext.ComponentId, ref newFrame);
        }
    }

    private static void InitializeNewElementReferenceCaptureFrame(ref DiffContext diffContext, ref RenderTreeFrame newFrame)
    {
        var newElementReference = ElementReference.CreateWithUniqueId(diffContext.Renderer.ElementReferenceContext);
        newFrame.ElementReferenceCaptureIdField = newElementReference.Id;
        newFrame.ElementReferenceCaptureActionField(newElementReference);
    }

    private static void InitializeNewComponentReferenceCaptureFrame(ref DiffContext diffContext, ref RenderTreeFrame newFrame)
    {
        ref var parentFrame = ref diffContext.NewTree[newFrame.ComponentReferenceCaptureParentFrameIndexField];
        if (parentFrame.FrameTypeField != RenderTreeFrameType.Component)
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

        newFrame.ComponentReferenceCaptureActionField(componentInstance);
    }

    private static void InitializeNewNamedEvent(ref DiffContext diffContext, int newTreeFrameIndex)
    {
        diffContext.BatchBuilder.AddNamedEvent(diffContext.ComponentId, newTreeFrameIndex, ref diffContext.NewTree[newTreeFrameIndex]);
    }

    private static void DisposeFramesInRange(RenderBatchBuilder batchBuilder, int componentId, RenderTreeFrame[] frames, int startIndex, int endIndexExcl)
    {
        for (var i = startIndex; i < endIndexExcl; i++)
        {
            ref var frame = ref frames[i];
            if (frame.FrameTypeField == RenderTreeFrameType.Component && frame.ComponentStateField != null)
            {
                batchBuilder.ComponentDisposalQueue.Enqueue(frame.ComponentIdField);
            }
            else if (frame.FrameTypeField == RenderTreeFrameType.Attribute && frame.AttributeEventHandlerIdField > 0)
            {
                batchBuilder.DisposedEventHandlerIds.Append(frame.AttributeEventHandlerIdField);
            }
            else if (frame.FrameTypeField == RenderTreeFrameType.NamedEvent)
            {
                batchBuilder.RemoveNamedEvent(componentId, i, ref frames[i]);
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
        public readonly StackObjectPool<Dictionary<object, KeyedItemInfo>> KeyedItemInfoDictionaryPool;
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
            KeyedItemInfoDictionaryPool = batchBuilder.KeyedItemInfoDictionaryPool;
            SiblingIndex = 0;
        }
    }
}
