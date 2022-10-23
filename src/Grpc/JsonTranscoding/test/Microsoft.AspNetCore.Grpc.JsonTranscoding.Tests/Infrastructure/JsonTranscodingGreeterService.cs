// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Grpc.Core;
using Transcoding;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.Infrastructure;

public class JsonTranscodingGreeterService : JsonTranscodingGreeter.JsonTranscodingGreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return base.SayHello(request, context);
    }
}
