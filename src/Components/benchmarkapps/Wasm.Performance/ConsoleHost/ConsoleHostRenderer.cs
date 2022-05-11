// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Wasm.Performance.ConsoleHost;

internal sealed class ConsoleHostRenderer : Renderer
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
