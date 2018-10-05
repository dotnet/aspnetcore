// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Rendering;

namespace Microsoft.AspNetCore.Blazor.Test.Helpers
{
    public class TestRenderer : Renderer
    {
        public TestRenderer(): this(new TestServiceProvider())
        {
        }

        public TestRenderer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public Action<RenderBatch> OnUpdateDisplay { get; set; }

        public List<CapturedBatch> Batches { get; }
            = new List<CapturedBatch>();

        public new int AssignRootComponentId(IComponent component)
            => base.AssignRootComponentId(component);

        public new void DispatchEvent(int componentId, int eventHandlerId, UIEventArgs args)
            => base.DispatchEvent(componentId, eventHandlerId, args);

        public T InstantiateComponent<T>() where T : IComponent
            => (T)InstantiateComponent(typeof(T));

        protected override void UpdateDisplay(in RenderBatch renderBatch)
        {
            OnUpdateDisplay?.Invoke(renderBatch);

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
