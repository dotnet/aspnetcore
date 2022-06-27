// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Greet;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger _logger;

    public GreeterService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GreeterService>();
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        _logger.LogInformation($"Sending hello to {request.Name}");
        return Task.FromResult(new HelloReply { Message = $"Hello {request.Name}" });
    }
}
