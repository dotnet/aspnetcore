// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers
{
    public class MockConnectionHandler : IConnectionHandler
    {
        public PipeOptions InputOptions { get; set; } = new PipeOptions();
        public PipeOptions OutputOptions { get; set; } = new PipeOptions();

        public IConnectionContext OnConnection(IConnectionInformation connectionInfo)
        {
            Input = connectionInfo.PipeFactory.Create(InputOptions ?? new PipeOptions());
            Output = connectionInfo.PipeFactory.Create(OutputOptions ?? new PipeOptions());

            return new TestConnectionContext
            {
                Input = Input.Writer,
                Output = Output.Reader,
            };
        }

        public IPipe Input { get; private set; }
        public IPipe Output { get; private set; }
        
        private class TestConnectionContext : IConnectionContext
        {
            public string ConnectionId { get; }
            public IPipeWriter Input { get; set; }
            public IPipeReader Output { get; set; }

            public void Abort(Exception ex)
            {
            }

            public void OnConnectionClosed(Exception ex)
            {
            }
        }
    }
}
