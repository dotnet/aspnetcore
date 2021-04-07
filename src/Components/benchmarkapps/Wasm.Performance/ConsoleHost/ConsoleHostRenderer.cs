// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Wasm.Performance.ConsoleHost
{
    internal class ConsoleHostRenderer : Renderer
    {
        public ConsoleHostRenderer(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
            : base(serviceProvider, loggerFactory)
        {
        }

        public override Dispatcher Dispatcher { get; } = new NullDispatcher();

        protected override void HandleException(Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            // ConsoleHost is only for profiling the .NET side of execution.
            // There isn't a real display to update.
            return Task.CompletedTask;
        }

        // Expose some protected APIs publicly
        public new int AssignRootComponentId(IComponent component)
            => base.AssignRootComponentId(component);

        public new Task RenderRootComponentAsync(int componentId)
            => base.RenderRootComponentAsync(componentId);
    }
}
