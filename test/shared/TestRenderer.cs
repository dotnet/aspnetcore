// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Test.Helpers
{
    public class TestRenderer : Renderer
    {
        public List<CapturedBatch> Batches { get; }
            = new List<CapturedBatch>();

        public new int AssignComponentId(IComponent component)
            => base.AssignComponentId(component);

        public new void DispatchEvent(int componentId, int eventHandlerId, UIEventArgs args)
            => base.DispatchEvent(componentId, eventHandlerId, args);

        protected override void UpdateDisplay(RenderBatch renderBatch)
        {
            var capturedBatch = new CapturedBatch();
            Batches.Add(capturedBatch);

            for (var i = 0; i < renderBatch.UpdatedComponents.Count; i++)
            {
                ref var renderTreeDiff = ref renderBatch.UpdatedComponents.Array[i];
                capturedBatch.AddDiff(renderTreeDiff);
            }

            // Clone other data, as underlying storage will get reused by later batches
            capturedBatch.ReferenceFrames = renderBatch.ReferenceFrames.ToArray();
            capturedBatch.DisposedComponentIDs = renderBatch.DisposedComponentIDs.ToList();
        }
    }
}
