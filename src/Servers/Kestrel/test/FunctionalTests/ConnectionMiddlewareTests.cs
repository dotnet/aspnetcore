// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Net;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;

#if SOCKETS
namespace Microsoft.AspNetCore.Server.Kestrel.Sockets.FunctionalTests;
#else
namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests;
#endif

public class ConnectionMiddlewareTests : LoggedTest
{
    [Fact]
    public async Task ThrowingSynchronousConnectionMiddlewareDoesNotCrashServer()
    {
        var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));
        listenOptions.Use(next => context => throw new Exception());

        var serviceContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(TestApp.EchoApp, serviceContext, listenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                // Will throw because the exception in the connection adapter will close the connection.
                await Assert.ThrowsAnyAsync<IOException>(async () =>
                {
                    await connection.Send(
                        "POST / HTTP/1.0",
                        "Content-Length: 1000",
                        "\r\n");

                    for (var i = 0; i < 1000; i++)
                    {
                        await connection.Send("a");
                        await Task.Delay(5);
                    }
                });
            }
        }
    }

    [Fact]
    public async Task DisposeAsyncAfterReplacingTransportClosesConnection()
    {
        var listenOptions = new ListenOptions(new IPEndPoint(IPAddress.Loopback, 0));

        var connectionCloseTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var mockDuplexPipe = new MockDuplexPipe();

        listenOptions.Use(next =>
        {
            return async context =>
            {
                context.Transport = mockDuplexPipe;
                await context.DisposeAsync();
                await connectionCloseTcs.Task;
            };
        });

        var serviceContext = new TestServiceContext(LoggerFactory);

        await using (var server = new TestServer(TestApp.EmptyApp, serviceContext, listenOptions))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.WaitForConnectionClose();
                connectionCloseTcs.SetResult();
            }
        }

        Assert.False(mockDuplexPipe.WasCompleted);
    }

    private class MockDuplexPipe : IDuplexPipe
    {
        public bool WasCompleted { get; private set; }

        public PipeReader Input => new MockPipeReader(this);

        public PipeWriter Output => new MockPipeWriter(this);

        private class MockPipeReader : PipeReader
        {
            private readonly MockDuplexPipe _duplexPipe;

            public MockPipeReader(MockDuplexPipe duplexPipe)
            {
                _duplexPipe = duplexPipe;
            }

            public override void AdvanceTo(SequencePosition consumed)
            {
                throw new NotImplementedException();
            }

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
            {
                throw new NotImplementedException();
            }

            public override void CancelPendingRead()
            {
                throw new NotImplementedException();
            }

            public override void Complete(Exception exception = null)
            {
                _duplexPipe.WasCompleted = true;
            }

            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public override bool TryRead(out ReadResult result)
            {
                throw new NotImplementedException();
            }
        }

        private class MockPipeWriter : PipeWriter
        {
            private readonly MockDuplexPipe _duplexPipe;

            public MockPipeWriter(MockDuplexPipe duplexPipe)
            {
                _duplexPipe = duplexPipe;
            }

            public override void Advance(int bytes)
            {
                throw new NotImplementedException();
            }

            public override void CancelPendingFlush()
            {
                throw new NotImplementedException();
            }

            public override void Complete(Exception exception = null)
            {
                _duplexPipe.WasCompleted = true;
            }

            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public override Memory<byte> GetMemory(int sizeHint = 0)
            {
                throw new NotImplementedException();
            }

            public override Span<byte> GetSpan(int sizeHint = 0)
            {
                throw new NotImplementedException();
            }
        }
    }
}

