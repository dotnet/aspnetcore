// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering;

internal sealed class NullDispatcher : Dispatcher
{
    public static readonly Dispatcher Instance = new NullDispatcher();

    private NullDispatcher()
    {
    }

    public override bool CheckAccess() => true;

    public override Task InvokeAsync(Action workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        workItem();
        return Task.CompletedTask;
    }

    public override Task InvokeAsync(Func<Task> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        return workItem();
    }

    public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        return Task.FromResult(workItem());
    }

    public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        return workItem();
    }
}
