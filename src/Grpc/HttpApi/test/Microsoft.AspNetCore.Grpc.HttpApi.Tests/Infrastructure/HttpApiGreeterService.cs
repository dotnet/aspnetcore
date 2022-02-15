// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Grpc.Core;
using HttpApi;

namespace Microsoft.AspNetCore.Grpc.HttpApi.Tests.Infrastructure;

public class HttpApiGreeterService : HttpApiGreeter.HttpApiGreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return base.SayHello(request, context);
    }
}
