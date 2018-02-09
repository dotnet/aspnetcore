// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Rendering
{
    internal class RenderBatchBuilder
    {
        public ArrayBuilder<RenderTreeEdit> EditsBuffer { get; } = new ArrayBuilder<RenderTreeEdit>();
        public ArrayBuilder<RenderTreeFrame> ReferenceFramesBuffer { get; } = new ArrayBuilder<RenderTreeFrame>();

        public Queue<int> ComponentRenderQueue { get; } = new Queue<int>();

        public Queue<int> ComponentDisposalQueue { get; } = new Queue<int>();

        public ArrayBuilder<RenderTreeDiff> UpdatedComponentDiffs { get; set; }
            = new ArrayBuilder<RenderTreeDiff>();

        private readonly ArrayBuilder<int> _disposedComponentIds = new ArrayBuilder<int>();

        private readonly ArrayBuilder<int> _disposedEventHandlerIds = new ArrayBuilder<int>();

        public ArrayRange<int> GetDisposedEventHandlerIds()
            => _disposedEventHandlerIds.ToRange();

        public void Clear()
        {
            EditsBuffer.Clear();
            ReferenceFramesBuffer.Clear();
            ComponentRenderQueue.Clear();
            UpdatedComponentDiffs.Clear();
            _disposedComponentIds.Clear();
            _disposedEventHandlerIds.Clear();
        }

        public RenderBatch ToBatch()
            => new RenderBatch(
                UpdatedComponentDiffs.ToRange(),
                ReferenceFramesBuffer.ToRange(),
                _disposedComponentIds.ToRange());

        public void AddDisposedComponentId(int componentId)
            => _disposedComponentIds.Append(componentId);

        public void AddDisposedEventHandlerId(int attributeEventHandlerId)
            => _disposedEventHandlerIds.Append(attributeEventHandlerId);
    }
}
