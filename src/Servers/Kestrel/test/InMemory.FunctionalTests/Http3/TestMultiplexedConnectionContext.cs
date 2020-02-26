// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public partial class Http3TestBase
    {
        private class TestMultiplexedConnectionContext : MultiplexedConnectionContext, IProtocolErrorCodeFeature
        {
            public readonly Channel<ConnectionContext> AcceptQueue = Channel.CreateUnbounded<ConnectionContext>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true
            });

            private readonly Http3TestBase _testBase;

            public TestMultiplexedConnectionContext(Http3TestBase testBase)
            {
                Features = new FeatureCollection();
                Features.Set<IProtocolErrorCodeFeature>(this);
                _testBase = testBase;
            }

            public override string ConnectionId { get; set; }

            public override IFeatureCollection Features { get; }

            public override IDictionary<object, object> Items { get; set; }

            public long Error { get; set; }

            public override void Abort()
            {
            }

            public override void Abort(ConnectionAbortedException abortReason)
            {
            }

            public override async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
            {
                while (await AcceptQueue.Reader.WaitToReadAsync())
                {
                    while (AcceptQueue.Reader.TryRead(out var connection))
                    {
                        return connection;
                    }
                }

                return null;
            }

            public override ValueTask<ConnectionContext> ConnectAsync(IFeatureCollection features = null, CancellationToken cancellationToken = default)
            {
                var inputPipeOptions = GetInputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);
                var outputPipeOptions = GetOutputPipeOptions(_testBase._serviceContext, _testBase._memoryPool, PipeScheduler.ThreadPool);

                var pair = DuplexPipe.CreateConnectionPair(inputPipeOptions, outputPipeOptions);

                var remoteStreamContext = new TestStreamContext(canRead: true, canWrite: false, pair.Application, pair.Transport);
                var localStreamContext = new TestStreamContext(canRead: false, canWrite: true, pair.Transport, pair.Application);

                _testBase.AcceptQueue.Writer.TryWrite(remoteStreamContext);

                return new ValueTask<ConnectionContext>(localStreamContext);
            }
        }
    }
}
