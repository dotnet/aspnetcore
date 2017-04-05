// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests.TestHelpers
{
    public class MockConnectionHandler : IConnectionHandler, IDisposable
    {
        private readonly PipeFactory _pipeFactory;

        public MockConnectionHandler()
        {
            _pipeFactory = new PipeFactory();
        }

        public IConnectionContext OnConnection(IConnectionInformation connectionInfo)
        {
            Assert.Null(Input);

            Input = _pipeFactory.Create();
            Output = _pipeFactory.Create();

            return new TestConnectionContext
            {
                Input = Input.Writer,
                Output = Output.Reader,
            };
        }

        public IPipe Input { get; private set; }
        public IPipe Output { get; private set; }

        public void Dispose()
        {
            Input?.Writer.Complete();
            _pipeFactory.Dispose();
        }

        private class TestConnectionContext : IConnectionContext
        {
            public string ConnectionId { get; }
            public IPipeWriter Input { get; set; }
            public IPipeReader Output { get; set; }

            public void OnConnectionClosed()
            {
                throw new NotImplementedException();
            }

            public Task StopAsync()
            {
                throw new NotImplementedException();
            }

            public void Abort(Exception ex)
            {
                throw new NotImplementedException();
            }

            public void Timeout()
            {
                throw new NotImplementedException();
            }
        }
    }
}
