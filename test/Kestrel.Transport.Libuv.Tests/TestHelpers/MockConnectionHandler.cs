// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Protocols.Features;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers
{
    public class MockConnectionHandler : IConnectionHandler
    {
        public Func<MemoryPool<byte>, PipeOptions> InputOptions { get; set; } = pool => new PipeOptions(pool);
        public Func<MemoryPool<byte>, PipeOptions> OutputOptions { get; set; } = pool => new PipeOptions(pool);

        public void OnConnection(IFeatureCollection features)
        {
            var connectionContext = new DefaultConnectionContext(features);

            var feature = connectionContext.Features.Get<IConnectionTransportFeature>();

            Input = new Pipe(InputOptions(feature.MemoryPool));
            Output = new Pipe(OutputOptions(feature.MemoryPool));

            connectionContext.Transport = new DuplexPipe(Input.Reader, Output.Writer);
            feature.Application = new DuplexPipe(Output.Reader, Input.Writer);
        }

        public Pipe Input { get; private set; }
        public Pipe Output { get; private set; }
    }
}
