// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Sockets.Client
{
    internal class PipelineConnection : IPipelineConnection
    {
        public IPipelineReader Input { get; }
        public IPipelineWriter Output { get; }

        public PipelineConnection(IPipelineReader input, IPipelineWriter output)
        {
            Input = input;
            Output = output;
        }

        public void Dispose()
        {
            Input.Complete();
            Output.Complete();
        }
    }
}
