// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    public class PipelineConnection : IPipelineConnection
    {
        public PipelineReaderWriter Input { get; }
        public PipelineReaderWriter Output { get; }

        IPipelineReader IPipelineConnection.Input => Input;
        IPipelineWriter IPipelineConnection.Output => Output;

        public PipelineConnection(PipelineReaderWriter input, PipelineReaderWriter output)
        {
            Input = input;
            Output = output;
        }

        public void Dispose()
        {
            Input.CompleteReader();
            Input.CompleteWriter();
            Output.CompleteReader();
            Output.CompleteWriter();
        }
    }
}
