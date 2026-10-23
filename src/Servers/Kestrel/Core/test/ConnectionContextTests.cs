// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class ConnectionContextTests
{
    [Fact]
    public void ParameterlessAbortCreateConnectionAbortedException()
    {
        var mockConnectionContext = new Mock<ConnectionContext> { CallBase = true };
        ConnectionAbortedException ex = null;

        mockConnectionContext.Setup(c => c.Abort(It.IsAny<ConnectionAbortedException>()))
                             .Callback<ConnectionAbortedException>(abortReason => ex = abortReason);

        mockConnectionContext.Object.Abort();

        Assert.NotNull(ex);
        Assert.Equal("The connection was aborted by the application via ConnectionContext.Abort().", ex.Message);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void DefaultConnectionContextDisposeAsyncAfterAbortDoesNotCrashProcess()
    {
        using var remoteHandle = RemoteExecutor.Invoke(static async () =>
        {
            ThreadPool.GetMinThreads(out var originalMinWorkerThreads, out var originalMinCompletionPortThreads);
            ThreadPool.GetMaxThreads(out var originalMaxWorkerThreads, out var originalMaxCompletionPortThreads);

            Assert.True(ThreadPool.SetMinThreads(1, originalMinCompletionPortThreads));
            Assert.True(ThreadPool.SetMaxThreads(1, originalMaxCompletionPortThreads));

            using var blockerStarted = new ManualResetEventSlim();
            using var releaseBlocker = new ManualResetEventSlim();

            try
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    blockerStarted.Set();
                    releaseBlocker.Wait();
                });

                Assert.True(blockerStarted.Wait(TimeSpan.FromSeconds(10)));

                var connection = new DefaultConnectionContext();
                connection.Abort();
                await connection.DisposeAsync();

                releaseBlocker.Set();

                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            finally
            {
                releaseBlocker.Set();
                ThreadPool.SetMaxThreads(originalMaxWorkerThreads, originalMaxCompletionPortThreads);
                ThreadPool.SetMinThreads(originalMinWorkerThreads, originalMinCompletionPortThreads);
            }
        });
    }
}
