// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

public class StubModelBinder : IModelBinder
{
    private readonly Func<ModelBindingContext, Task> _callback;

    public StubModelBinder()
    {
        _callback = context => Task.CompletedTask;
    }

    public StubModelBinder(ModelBindingResult result)
    {
        _callback = context =>
        {
            context.Result = result;
            return Task.CompletedTask;
        };
    }

    public StubModelBinder(Action<ModelBindingContext> callback)
    {
        _callback = context =>
        {
            callback(context);
            return Task.CompletedTask;
        };
    }

    public StubModelBinder(Func<ModelBindingContext, ModelBindingResult> callback)
    {
        _callback = context =>
        {
            var result = callback.Invoke(context);
            context.Result = result;
            return Task.CompletedTask;
        };
    }

    public StubModelBinder(Func<ModelBindingContext, Task<ModelBindingResult>> callback)
    {
        _callback = async context =>
        {
            var result = await callback.Invoke(context);
            context.Result = result;
        };
    }

    public int BindModelCount { get; set; }

    public IModelBinder Object => this;

    public virtual async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        BindModelCount += 1;

        ArgumentNullException.ThrowIfNull(bindingContext);

        Debug.Assert(bindingContext.Result == ModelBindingResult.Failed());
        await _callback.Invoke(bindingContext);
    }
}
