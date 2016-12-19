// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Sockets.Client
{
    // TODO: Move to System.IO.Pipelines
    public static class PipelineFactoryExtensions
    {
        // TODO: Use a named tuple? Though there aren't really good names for these ... client/server? left/right?
        public static Tuple<IPipelineConnection, IPipelineConnection> CreatePipelinePair(this PipelineFactory self)
        {
            // Create a pair of pipelines for "Server" and "Client"
            var clientToServer = self.Create();
            var serverToClient = self.Create();

            // "Server" reads from clientToServer and writes to serverToClient
            var server = new PipelineConnection(
                input: clientToServer,
                output: serverToClient);

            // "Client" reads from serverToClient and writes to clientToServer
            var client = new PipelineConnection(
                input: serverToClient,
                output: clientToServer);

            return Tuple.Create((IPipelineConnection)server, (IPipelineConnection)client);
        }
    }
}
