// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Microsoft.AspNetCore.Components.WebAssembly.Rendering
{
    internal class NullDispatcher : Dispatcher
    {
        public static readonly Dispatcher Instance = new NullDispatcher();

        private NullDispatcher()
        {
        }

        public override bool CheckAccess() => true;

        public override Task InvokeAsync(Action workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            workItem();
            return Task.CompletedTask;
        }

        public override Task InvokeAsync(Func<Task> workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            return workItem();
        }

        public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            return Task.FromResult(workItem());
        }

        public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        {
            if (workItem is null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            return workItem();
        }
    }
}
