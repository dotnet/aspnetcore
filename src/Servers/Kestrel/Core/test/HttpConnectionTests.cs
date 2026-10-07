// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class HttpConnectionTests
{
    [Fact]
    public async Task WriteDataRateTimeoutAbortsConnection()
    {
        var connectionContext = new TestConnectionContext();
        var pipe = new Pipe();

        var httpConnectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: new TestServiceContext(),
            connectionContext: connectionContext,
            connectionFeatures: new FeatureCollection(),
            transport: new DuplexPipe(pipe.Reader, pipe.Writer));

        var httpConnection = new HttpConnection(httpConnectionContext);

        var aborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var http1Connection = new Http1Connection(httpConnectionContext);

        httpConnection.Initialize(http1Connection);
        http1Connection.Reset();
        http1Connection.RequestAborted.Register(() =>
        {
            aborted.SetResult();
        });

        httpConnection.OnTimeout(TimeoutReason.WriteDataRate);

        var abortReason = Assert.Single(connectionContext.AbortReasons);
        Assert.Equal(CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied, abortReason.Message);

        await aborted.Task.DefaultTimeout();
    }
}
