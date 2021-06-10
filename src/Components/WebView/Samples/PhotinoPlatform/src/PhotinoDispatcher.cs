// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PhotinoNET;

namespace Microsoft.AspNetCore.Components.WebView.Photino
{
    internal class PhotinoDispatcher : Dispatcher
    {
        private readonly PhotinoWindow _window;
        private readonly int _uiThreadId;
        private readonly MethodInfo _invokeMethodInfo;

        public PhotinoDispatcher(PhotinoWindow window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));

            _uiThreadId = (int)_window.GetType()
                .GetField("_managedThreadId", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(_window)!;

            _invokeMethodInfo = _window.GetType()
                .GetMethod("Invoke", BindingFlags.NonPublic | BindingFlags.Instance)!;
        }

        public override bool CheckAccess()
        {
            return Environment.CurrentManagedThreadId == _uiThreadId;
        }

        public override Task InvokeAsync(Action workItem)
        {
            if (CheckAccess())
            {
                workItem();
                return Task.CompletedTask;
            }
            else
            {
                var tcs = new TaskCompletionSource();
                _invokeMethodInfo.Invoke(_window, new Action[] {() => {
                    try
                    {
                        workItem();
                        tcs.SetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }});
                return tcs.Task;
            }
        }

        public override Task InvokeAsync(Func<Task> workItem)
        {
            if (CheckAccess())
            {
                return workItem();
            }
            else
            {
                var tcs = new TaskCompletionSource();
                _invokeMethodInfo.Invoke(_window, new Action[]
                {
                    () => { _ = RunWorkItemAsync(); }
                });
                return tcs.Task;

                async Task RunWorkItemAsync()
                {
                    try
                    {
                        await workItem();
                        tcs.SetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }
            }
        }
        
        public override Task<TResult> InvokeAsync<TResult>(Func<TResult> workItem)
        {
            if (CheckAccess())
            {
                return Task.FromResult(workItem());
            }
            else
            {
                var tcs = new TaskCompletionSource<TResult>();
                _invokeMethodInfo.Invoke(_window, new Action[] {() => {
                    try
                    {
                        var result = workItem();
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }});
                return tcs.Task;
            }
        }

        public override Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> workItem)
        {
            if (CheckAccess())
            {
                return workItem();
            }
            else
            {
                var tcs = new TaskCompletionSource<TResult>();
                _invokeMethodInfo.Invoke(_window, new Action[]
                {
                    () => { _ = RunWorkItemAsync(); }
                });
                return tcs.Task;

                async Task RunWorkItemAsync()
                {
                    try
                    {
                        var result = await workItem();
                        tcs.SetResult(result);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }
            }
        }
    }
}
