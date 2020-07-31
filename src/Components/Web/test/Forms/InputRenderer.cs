// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;

namespace Microsoft.AspNetCore.Components.Forms
{
    internal static class InputRenderer
    {
        public static async Task<TComponent> RenderAndGetComponent<TValue, TComponent>(TestInputHostComponent<TValue, TComponent> hostComponent) 
        where TComponent : InputBase<TValue>
        {
            var testRenderer = new TestRenderer();
            var componentId = testRenderer.AssignRootComponentId(hostComponent);
            await testRenderer.RenderRootComponentAsync(componentId);
            return FindComponent<TComponent>(testRenderer.Batches.Single());
        }

        private static TComponent FindComponent<TComponent>(CapturedBatch batch)
            => batch.ReferenceFrames
                    .Where(f => f.FrameType == RenderTreeFrameType.Component)
                    .Select(f => f.Component)
                    .OfType<TComponent>()
                    .Single();
    }
}
