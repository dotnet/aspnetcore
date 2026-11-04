// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Wasm.Performance.ConsoleHost;

internal sealed class NullDispatcher : Dispatcher
{
    public override bool CheckAccess()
        => true;

    public override Task InvokeAsync(Action workItem)
    {
        workItem();
        return Task.CompletedTask;
    }

    public override Task InvokeAsync(Func<Task> workItem)
        => workItem();

    public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        => Task.FromResult(workItem());

    public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        => workItem();
}
