// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using static System.Windows.Threading.Dispatcher;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
    internal class WpfDispatcher : Dispatcher
    {
        public static Dispatcher Instance { get; } = new WpfDispatcher();

        private static Action<Exception> RethrowException = exception =>
            ExceptionDispatchInfo.Capture(exception).Throw();

        public override bool CheckAccess()
            => CurrentDispatcher.CheckAccess();

        public override async Task InvokeAsync(Action workItem)
        {
            try
            {
                if (CurrentDispatcher.CheckAccess())
                {
                    workItem();
                }
                else
                {
                    await CurrentDispatcher.InvokeAsync(workItem);
                }
            }
            catch (Exception ex)
            {
                // TODO: Determine whether this is the right kind of rethrowing pattern
                // You do have to do something like this otherwise unhandled exceptions
                // throw from inside Dispatcher.InvokeAsync are simply lost.
                _ = CurrentDispatcher.BeginInvoke(RethrowException, ex);
                throw;
            }
        }

        public override async Task InvokeAsync(Func<Task> workItem)
        {
            try
            {
                if (CurrentDispatcher.CheckAccess())
                {
                    await workItem();
                }
                else
                {
                    await CurrentDispatcher.InvokeAsync(workItem);
                }
            }
            catch (Exception ex)
            {
                // TODO: Determine whether this is the right kind of rethrowing pattern
                // You do have to do something like this otherwise unhandled exceptions
                // throw from inside Dispatcher.InvokeAsync are simply lost.
                _ = CurrentDispatcher.BeginInvoke(RethrowException, ex);
                throw;
            }
        }

        public override async Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        {
            try
            {
                if (CurrentDispatcher.CheckAccess())
                {
                    return workItem();
                }
                else
                {
                    return await CurrentDispatcher.InvokeAsync(workItem);
                }
            }
            catch (Exception ex)
            {
                // TODO: Determine whether this is the right kind of rethrowing pattern
                // You do have to do something like this otherwise unhandled exceptions
                // throw from inside Dispatcher.InvokeAsync are simply lost.
                _ = CurrentDispatcher.BeginInvoke(RethrowException, ex);
                throw;
            }
        }

        public override async Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        {
            try
            {
                if (CurrentDispatcher.CheckAccess())
                {
                    return await workItem();
                }
                else
                {
                    return await CurrentDispatcher.InvokeAsync(workItem).Task.Unwrap();
                }
            }
            catch (Exception ex)
            {
                // TODO: Determine whether this is the right kind of rethrowing pattern
                // You do have to do something like this otherwise unhandled exceptions
                // throw from inside Dispatcher.InvokeAsync are simply lost.
                _ = CurrentDispatcher.BeginInvoke(RethrowException, ex);
                throw;
            }
        }
    }
}
