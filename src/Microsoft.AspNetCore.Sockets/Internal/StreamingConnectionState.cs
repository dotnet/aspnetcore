// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    public class StreamingConnectionState : ConnectionState
    {
        public new StreamingConnection Connection => (StreamingConnection)base.Connection;
        public IPipelineConnection Application { get; }

        public StreamingConnectionState(StreamingConnection connection, IPipelineConnection application) : base(connection)
        {
            Application = application;
        }

        public override void Dispose()
        {
            Connection.Dispose();
            Application.Dispose();
        }

        public override void TerminateTransport(Exception innerException)
        {
            Connection.Transport.Output.Complete(innerException);
            Connection.Transport.Input.Complete(innerException);
        }
    }
}
