// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Test.Helpers
{
    public class CapturedBatch
    {
        public IDictionary<int, List<RenderTreeDiff>> DiffsByComponentId { get; }
            = new Dictionary<int, List<RenderTreeDiff>>();

        public IList<RenderTreeDiff> DiffsInOrder { get; }
            = new List<RenderTreeDiff>();

        public IList<int> DisposedComponentIDs { get; set; }
        public RenderTreeFrame[] ReferenceFrames { get; set; }

        public IEnumerable<RenderTreeFrame> GetComponentFrames<T>() where T : IComponent
            => ReferenceFrames.Where(f => f.FrameType == RenderTreeFrameType.Component && f.Component is T);

        public IEnumerable<RenderTreeDiff> GetComponentDiffs<T>() where T : IComponent
            => GetComponentFrames<T>().SelectMany(f => DiffsByComponentId[f.ComponentId]);

        internal void AddDiff(RenderTreeDiff diff)
        {
            var componentId = diff.ComponentId;
            if (!DiffsByComponentId.ContainsKey(componentId))
            {
                DiffsByComponentId.Add(componentId, new List<RenderTreeDiff>());
            }

            // Clone the diff, because its underlying storage will get reused in subsequent batches
            var cloneBuilder = new ArrayBuilder<RenderTreeEdit>();
            cloneBuilder.Append(diff.Edits.ToArray(), 0, diff.Edits.Count);
            var diffClone = new RenderTreeDiff(
                diff.ComponentId,
                cloneBuilder.ToSegment(0, diff.Edits.Count));
            DiffsByComponentId[componentId].Add(diffClone);
            DiffsInOrder.Add(diffClone);
        }
    }
}
