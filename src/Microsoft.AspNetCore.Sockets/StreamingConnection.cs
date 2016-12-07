// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Sockets
{
    public class StreamingConnection : Connection
    {
        public override ConnectionMode Mode => ConnectionMode.Streaming;

        public IPipelineConnection Transport { get; set; }

        public StreamingConnection(string id, IPipelineConnection transport) : base(id)
        {
            Transport = transport;
        }

        public override void Dispose()
        {
            Transport.Dispose();
        }
    }
}
