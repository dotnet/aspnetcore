// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Wasm.Performance.ConsoleHost
{
    internal class NullDispatcher : Dispatcher
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
}
