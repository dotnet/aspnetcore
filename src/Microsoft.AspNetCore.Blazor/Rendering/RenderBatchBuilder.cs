// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Rendering
{
    internal class RenderBatchBuilder
    {
        private ArrayBuilder<RenderTreeDiff> _updatedComponentDiffs = new ArrayBuilder<RenderTreeDiff>();
        private ArrayBuilder<int> _disposedComponentIds = new ArrayBuilder<int>();
        private ArrayBuilder<int> _disposedEventHandlerIds = new ArrayBuilder<int>();

        public int ReserveUpdatedComponentSlotId()
        {
            int id = _updatedComponentDiffs.Count;
            _updatedComponentDiffs.Append(default);
            return id;
        }

        public void SetUpdatedComponent(int updatedComponentSlotId, RenderTreeDiff diff)
            => _updatedComponentDiffs.Overwrite(updatedComponentSlotId, diff);

        public ArrayRange<int> GetDisposedEventHandlerIds()
            => _disposedEventHandlerIds.ToRange();

        public void Clear()
        {
            _updatedComponentDiffs.Clear();
            _disposedComponentIds.Clear();
            _disposedEventHandlerIds.Clear();
        }

        public RenderBatch ToBatch()
            => new RenderBatch(
                _updatedComponentDiffs.ToRange(),
                _disposedComponentIds.ToRange());

        public void AddDisposedComponent(int componentId)
            => _disposedComponentIds.Append(componentId);

        public void AddDisposedEventHandlerId(int attributeEventHandlerId)
            => _disposedEventHandlerIds.Append(attributeEventHandlerId);
    }
}
