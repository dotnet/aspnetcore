// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    internal class RenderBatchBuilder
    {
        // Primary result data
        public ArrayBuilder<RenderTreeDiff> UpdatedComponentDiffs { get; } = new ArrayBuilder<RenderTreeDiff>();
        public ArrayBuilder<int> DisposedComponentIds { get; } = new ArrayBuilder<int>();
        public ArrayBuilder<int> DisposedEventHandlerIds { get; } = new ArrayBuilder<int>();

        // Buffers referenced by UpdatedComponentDiffs
        public ArrayBuilder<RenderTreeEdit> EditsBuffer { get; } = new ArrayBuilder<RenderTreeEdit>();
        public ArrayBuilder<RenderTreeFrame> ReferenceFramesBuffer { get; } = new ArrayBuilder<RenderTreeFrame>();

        // State of render pipeline
        public Queue<RenderQueueEntry> ComponentRenderQueue { get; } = new Queue<RenderQueueEntry>();
        public Queue<int> ComponentDisposalQueue { get; } = new Queue<int>();

        // Scratch data structure for understanding attribute diffs.
        public Dictionary<string, int> AttributeDiffSet { get; } = new Dictionary<string, int>();

        public void Clear()
        {
            EditsBuffer.Clear();
            ReferenceFramesBuffer.Clear();
            ComponentRenderQueue.Clear();
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
    }
}
