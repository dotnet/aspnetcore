// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class RejectionConnection : IConnectionContext
    {
        private readonly IKestrelTrace _log;
        private readonly IPipe _input;
        private readonly IPipe _output;

        public RejectionConnection(IPipe input, IPipe output, string connectionId, ServiceContext serviceContext)
        {
            ConnectionId = connectionId;
            _log = serviceContext.Log;
            _input = input;
            _output = output;
        }

        public string ConnectionId { get; }
        public IPipeWriter Input => _input.Writer;
        public IPipeReader Output => _output.Reader;

        public void Reject()
        {
            KestrelEventSource.Log.ConnectionRejected(ConnectionId);
            _log.ConnectionRejected(ConnectionId);
            _input.Reader.Complete();
            _output.Writer.Complete();
        }

        // TODO: Remove these (https://github.com/aspnet/KestrelHttpServer/issues/1772)
        void IConnectionContext.OnConnectionClosed(Exception ex)
        {
        }

        void IConnectionContext.Abort(Exception ex)
        {
        }
    }
}
