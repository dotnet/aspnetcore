// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Protocols.Features;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers
{
    public class MockConnectionHandler : IConnectionHandler
    {
        public Func<BufferPool, PipeOptions> InputOptions { get; set; } = pool => new PipeOptions(pool);
        public Func<BufferPool, PipeOptions> OutputOptions { get; set; } = pool => new PipeOptions(pool);

        public void OnConnection(IFeatureCollection features)
        {
            var connectionContext = new DefaultConnectionContext(features);

            Input = new Pipe(InputOptions(connectionContext.BufferPool));
            Output = new Pipe(InputOptions(connectionContext.BufferPool));

            var feature = connectionContext.Features.Get<IConnectionTransportFeature>();

            connectionContext.Transport = new PipeConnection(Input.Reader, Output.Writer);
            feature.Application = new PipeConnection(Output.Reader, Input.Writer);
        }

        public IPipe Input { get; private set; }
        public IPipe Output { get; private set; }
    }
}
