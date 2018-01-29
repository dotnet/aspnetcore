// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Rendering
{
    internal class RenderBatchBuilder
    {
        private ArrayBuilder<RenderTreeDiff> _updatedComponentDiffs = new ArrayBuilder<RenderTreeDiff>();

        public int ReserveUpdatedComponentSlotId()
        {
            int id = _updatedComponentDiffs.Count;
            _updatedComponentDiffs.Append(default);
            return id;
        }

        public void SetUpdatedComponent(int updatedComponentSlotId, RenderTreeDiff diff)
            => _updatedComponentDiffs.Overwrite(updatedComponentSlotId, diff);

        public void Clear()
            => _updatedComponentDiffs.Clear();

        public RenderBatch ToBatch()
            => new RenderBatch(
                _updatedComponentDiffs.ToRange());
    }
}
