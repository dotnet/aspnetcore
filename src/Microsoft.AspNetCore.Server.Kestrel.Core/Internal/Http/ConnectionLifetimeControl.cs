// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public class ConnectionLifetimeControl
    {
        public ConnectionLifetimeControl(
            string connectionId,
            IPipeReader outputPipeReader,
            OutputProducer outputProducer,
            IKestrelTrace log)
        {
            ConnectionId = connectionId;
            OutputReader = outputPipeReader;
            OutputProducer = outputProducer;
            Log = log;
        }

        private string ConnectionId { get; }
        private IPipeReader OutputReader { get; }
        private OutputProducer OutputProducer { get; }
        private IKestrelTrace Log { get; }

        public void End(ProduceEndType endType)
        {
            switch (endType)
            {
                case ProduceEndType.ConnectionKeepAlive:
                    Log.ConnectionKeepAlive(ConnectionId);
                    break;
                case ProduceEndType.SocketShutdown:
                    OutputReader.CancelPendingRead();
                    goto case ProduceEndType.SocketDisconnect;
                case ProduceEndType.SocketDisconnect:
                    OutputProducer.Dispose();
                    Log.ConnectionDisconnect(ConnectionId);
                    break;
            }
        }
    }
}
