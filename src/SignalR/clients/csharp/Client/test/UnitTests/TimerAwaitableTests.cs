// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
{
    public class TimerAwaitableTests
    {
        [Fact]
        [QuarantinedTest]
        public void FinalizerRunsIfTimerAwaitableReferencesObject()
        {
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            UseTimerAwaitableAndUnref(tcs);

            var timeout = tcs.Task.OrTimeout();

            // Make sure it *really* cleans up
            while (!timeout.IsCompleted)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // Make sure the finalizer runs
            Assert.True(timeout.IsCompletedSuccessfully);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UseTimerAwaitableAndUnref(TaskCompletionSource<object> tcs)
        {
            _ = new ObjectWithTimerAwaitable(tcs).Start();
        }
    }

    // This object holds onto a TimerAwaitable referencing the callback (the async continuation is the callback)
    // it also has a finalizer that triggers a tcs so callers can be notified when this object is being cleaned up.
    public class ObjectWithTimerAwaitable
    {
        private readonly TimerAwaitable _timer;
        private readonly TaskCompletionSource<object> _tcs;

        public ObjectWithTimerAwaitable(TaskCompletionSource<object> tcs)
        {
            _tcs = tcs;
            _timer = new TimerAwaitable(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            _timer.Start();
        }

        public async Task Start()
        {
            using (_timer)
            {
                while (await _timer)
                {
                }
            }
        }

        ~ObjectWithTimerAwaitable()
        {
            _tcs.TrySetResult(null);
        }
    }
}
