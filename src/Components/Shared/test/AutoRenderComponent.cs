// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Test.Helpers;

public abstract class AutoRenderComponent : IComponent
{
    private RenderHandle _renderHandle;

    public void Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    public virtual Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        TriggerRender();
        return Task.CompletedTask;
    }

    // We do it this way so that we don't have to be doing renderer.Invoke on each and every test.
    public void TriggerRender()
    {
        var t = _renderHandle.Dispatcher.InvokeAsync(() => _renderHandle.Render(BuildRenderTree));
        // This should always be run synchronously
        Assert.True(t.IsCompleted);
        if (t.IsFaulted)
        {
            var exception = t.Exception.Flatten().InnerException;
            while (exception is AggregateException e)
            {
                exception = e.InnerException;
            }
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    protected abstract void BuildRenderTree(RenderTreeBuilder builder);
}
