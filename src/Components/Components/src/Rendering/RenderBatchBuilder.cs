// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Rendering
{
    /// <summary>
    /// Collects the data produced by the rendering system during the course
    /// of rendering a single batch. This tracks both the final output data
    /// and the intermediate states (such as the queue of components still to
    /// be rendered).
    /// </summary>
    internal class RenderBatchBuilder : IDisposable
    {
        // A value that, if changed, causes expiry of all ParameterView instances issued
        // for this RenderBatchBuilder. This is to prevent invalid reads from arrays that
        // may have been returned to the shared pool.
        private int _parameterViewValidityStamp;

        // Primary result data
        public ArrayBuilder<RenderTreeDiff> UpdatedComponentDiffs { get; } = new ArrayBuilder<RenderTreeDiff>();
        public ArrayBuilder<int> DisposedComponentIds { get; } = new ArrayBuilder<int>();
        public ArrayBuilder<ulong> DisposedEventHandlerIds { get; } = new ArrayBuilder<ulong>();

        // Buffers referenced by UpdatedComponentDiffs
        public ArrayBuilder<RenderTreeEdit> EditsBuffer { get; } = new ArrayBuilder<RenderTreeEdit>(64);
        public ArrayBuilder<RenderTreeFrame> ReferenceFramesBuffer { get; } = new ArrayBuilder<RenderTreeFrame>(64);

        // State of render pipeline
        public Queue<RenderQueueEntry> ComponentRenderQueue { get; } = new Queue<RenderQueueEntry>();
        public Queue<int> ComponentDisposalQueue { get; } = new Queue<int>();

        // Scratch data structure for understanding attribute diffs.
        public Dictionary<string, int> AttributeDiffSet { get; } = new Dictionary<string, int>();

        public int ParameterViewValidityStamp => _parameterViewValidityStamp;

        internal StackObjectPool<Dictionary<object, KeyedItemInfo>> KeyedItemInfoDictionaryPool { get; }
            = new StackObjectPool<Dictionary<object, KeyedItemInfo>>(maxPreservedItems: 10, () => new Dictionary<object, KeyedItemInfo>());

        public void ClearStateForCurrentBatch()
        {
            // This method is used to reset the builder back to a default state so it can
            // begin building the next batch. That means clearing all the tracked state, but
            // *not* clearing ComponentRenderQueue because that may hold information about
            // the next batch we want to build. We shouldn't ever need to clear
            // ComponentRenderQueue explicitly, because it gets cleared as an aspect of
            // processing the render queue.

            EditsBuffer.Clear();
            ReferenceFramesBuffer.Clear();
            UpdatedComponentDiffs.Clear();
            DisposedComponentIds.Clear();
            DisposedEventHandlerIds.Clear();
            AttributeDiffSet.Clear();
        }

        public RenderBatch ToBatch()
            => new RenderBatch(
                UpdatedComponentDiffs.ToRange(),
                ReferenceFramesBuffer.ToRange(),
                DisposedComponentIds.ToRange(),
                DisposedEventHandlerIds.ToRange());

        public void InvalidateParameterViews()
        {
            // Wrapping is fine because all that matters is whether a snapshotted value matches
            // the current one. There's no plausible case where it wraps around and happens to
            // increment all the way back to a previously-snapshotted value on the exact same
            // call that's checking the value.
            if (_parameterViewValidityStamp == int.MaxValue)
            {
                _parameterViewValidityStamp = int.MinValue;
            }
            else
            {
                _parameterViewValidityStamp++;
            }
        }

        public void Dispose()
        {
            EditsBuffer.Dispose();
            ReferenceFramesBuffer.Dispose();
            UpdatedComponentDiffs.Dispose();
            DisposedComponentIds.Dispose();
            DisposedEventHandlerIds.Dispose();
        }
    }
}
