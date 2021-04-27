// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using WindowsDispatcher = System.Windows.Threading.Dispatcher;

namespace Microsoft.AspNetCore.Components.WebView.Wpf
{
    internal sealed class WpfDispatcher : Dispatcher
    {
        private readonly WindowsDispatcher _windowsDispatcher;

        private WpfDispatcher(WindowsDispatcher windowsDispatcher)
        {
            _windowsDispatcher = windowsDispatcher ?? throw new ArgumentNullException(nameof(windowsDispatcher));
        }

        public static Dispatcher Instance { get; } = new WpfDispatcher(Application.Current.Dispatcher);

        private static Action<Exception> RethrowException = exception =>
            ExceptionDispatchInfo.Capture(exception).Throw();

        public override bool CheckAccess()
            => _windowsDispatcher.CheckAccess();

        public override async Task InvokeAsync(Action workItem)
        {
            try
            {
                if (_windowsDispatcher.CheckAccess())
                {
                    workItem();
                }
                else
                {
                    await _windowsDispatcher.InvokeAsync(workItem);
                }
            }
            catch (Exception ex)
            {
                // TODO: Determine whether this is the right kind of rethrowing pattern
                // You do have to do something like this otherwise unhandled exceptions
                // throw from inside Dispatcher.InvokeAsync are simply lost.
                _ = _windowsDispatcher.BeginInvoke(RethrowException, ex);
                throw;
            }
        }

        public override async Task InvokeAsync(Func<Task> workItem)
        {
            try
            {
                if (_windowsDispatcher.CheckAccess())
                {
                    await workItem();
                }
                else
                {
                    await _windowsDispatcher.InvokeAsync(workItem);
                }
            }
            catch (Exception ex)
            {
                // TODO: Determine whether this is the right kind of rethrowing pattern
                // You do have to do something like this otherwise unhandled exceptions
                // throw from inside Dispatcher.InvokeAsync are simply lost.
                _ = _windowsDispatcher.BeginInvoke(RethrowException, ex);
                throw;
            }
        }

        public override async Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        {
            try
            {
                if (_windowsDispatcher.CheckAccess())
                {
                    return workItem();
                }
                else
                {
                    return await _windowsDispatcher.InvokeAsync(workItem);
                }
            }
            catch (Exception ex)
            {
                // TODO: Determine whether this is the right kind of rethrowing pattern
                // You do have to do something like this otherwise unhandled exceptions
                // throw from inside Dispatcher.InvokeAsync are simply lost.
                _ = _windowsDispatcher.BeginInvoke(RethrowException, ex);
                throw;
            }
        }

        public override async Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        {
            try
            {
                if (_windowsDispatcher.CheckAccess())
                {
                    return await workItem();
                }
                else
                {
                    return await _windowsDispatcher.InvokeAsync(workItem).Task.Unwrap();
                }
            }
            catch (Exception ex)
            {
                // TODO: Determine whether this is the right kind of rethrowing pattern
                // You do have to do something like this otherwise unhandled exceptions
                // throw from inside Dispatcher.InvokeAsync are simply lost.
                _ = _windowsDispatcher.BeginInvoke(RethrowException, ex);
                throw;
            }
        }
    }
}
