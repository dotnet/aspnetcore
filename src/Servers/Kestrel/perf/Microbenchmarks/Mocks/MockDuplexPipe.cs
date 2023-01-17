// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

internal sealed class MockDuplexPipe : IDuplexPipe
{
    public MockDuplexPipe(PipeReader input, PipeWriter output)
    {
        Input = input;
        Output = output;
    }

    public PipeReader Input { get; }
    public PipeWriter Output { get; }
}
