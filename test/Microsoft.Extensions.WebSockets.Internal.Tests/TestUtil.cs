// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.WebSockets.Internal.Tests
{
    internal static class TestUtil
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

        public static Task OrTimeout(this Task task) => OrTimeout(task, DefaultTimeout);
        public static Task<T> OrTimeout<T>(this Task<T> task) => OrTimeout(task, DefaultTimeout);

        public static async Task OrTimeout(this Task task, TimeSpan timeout)
        {
            var completed = await Task.WhenAny(task, CreateTimeoutTask());
            Assert.Same(completed, task);
        }

        public static async Task<T> OrTimeout<T>(this Task<T> task, TimeSpan timeout)
        {
            var completed = await Task.WhenAny(task, CreateTimeoutTask());
            Assert.Same(task, completed);
            return task.Result;
        }

        public static Task CreateTimeoutTask() => CreateTimeoutTask(DefaultTimeout);

        public static Task CreateTimeoutTask(TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<object>();
            CreateTimeoutToken(timeout).Register(() => tcs.TrySetCanceled());
            return tcs.Task;
        }

        public static CancellationToken CreateTimeoutToken() => CreateTimeoutToken(DefaultTimeout);

        public static CancellationToken CreateTimeoutToken(TimeSpan timeout)
        {
            if (Debugger.IsAttached)
            {
                return CancellationToken.None;
            }
            else
            {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(timeout);
                return cts.Token;
            }
        }
    }
}
