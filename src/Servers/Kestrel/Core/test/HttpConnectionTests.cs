// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class HttpConnectionTests
    {
        [Fact]
        public async Task WriteDataRateTimeoutAbortsConnection()
        {
            var mockConnectionContext = new Mock<ConnectionContext>();

            var httpConnectionContext = TestContextFactory.CreateHttpConnectionContext(
                serviceContext: new TestServiceContext(),
                connectionContext: mockConnectionContext.Object,
                connectionFeatures: new FeatureCollection(),
                transport: new DuplexPipe(Mock.Of<PipeReader>(), Mock.Of<PipeWriter>()));

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

            mockConnectionContext
                .Verify(c => c.Abort(It.Is<ConnectionAbortedException>(ex => ex.Message == CoreStrings.ConnectionTimedBecauseResponseMininumDataRateNotSatisfied)),
                    Times.Once);

            await aborted.Task.DefaultTimeout();
        }
    }
}
