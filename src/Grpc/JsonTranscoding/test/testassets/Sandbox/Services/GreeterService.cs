// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Greet;
using Grpc.Core;

namespace Server;

public class JsonTranscodingGreeterService : Transcoding.JsonTranscodingGreeter.JsonTranscodingGreeterBase
{

}

public class GreeterService : Greeter.GreeterBase
{
    private readonly ILogger _logger;

    public GreeterService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<GreeterService>();
    }

    /// <summary>
    /// Say hello.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        _logger.LogInformation($"Sending hello to {request.Name}");
        return Task.FromResult(new HelloReply { Message = $"Hello {request.Name}" });
    }

    public override Task<HelloReply> SayHelloFrom(HelloRequestFrom request, ServerCallContext context)
    {
        _logger.LogInformation($"Sending hello to {request.Name} from {request.From}");
        return Task.FromResult(new HelloReply { Message = $"Hello {request.Name} from {request.From}" });
    }
}
