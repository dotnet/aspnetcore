// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class LoggingThreadPoolTests
    {
        [Fact]
        public void TcsContinuationErrorsDontGetLoggedAsGeneralErrors()
        {
            var testLogger = new TestApplicationErrorLogger();
            var testKestrelTrace = new TestKestrelTrace(testLogger);
            var threadPool = new LoggingThreadPool(testKestrelTrace);

            var completeTcs = new TaskCompletionSource<object>();
            ThrowSynchronously(completeTcs.Task);
            threadPool.Complete(completeTcs);

            var errorTcs = new TaskCompletionSource<object>();
            ThrowSynchronously(errorTcs.Task);
            threadPool.Error(errorTcs, new Exception());

            var cancelTcs = new TaskCompletionSource<object>();
            ThrowSynchronously(cancelTcs.Task);
            threadPool.Cancel(cancelTcs);

            Assert.Throws<AggregateException>(() =>
                Task.WhenAll(completeTcs.Task, errorTcs.Task, cancelTcs.Task).Wait());

            Assert.Equal(0, testLogger.TotalErrorsLogged);
        }

        private void ThrowSynchronously(Task task)
        {
            task.ContinueWith(_ =>
            {
                throw new Exception();
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}
