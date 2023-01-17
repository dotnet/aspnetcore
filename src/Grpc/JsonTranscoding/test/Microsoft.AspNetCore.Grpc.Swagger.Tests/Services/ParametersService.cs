// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Grpc.Core;
using Params;

namespace Microsoft.AspNetCore.Grpc.Swagger.Tests.Services;

public class ParametersService : Params.Parameters.ParametersBase
{
    public override Task<ParamResponse> DemoParametersOne(RequestOne requestId, ServerCallContext ctx)
    {
        return Task.FromResult(new ParamResponse { Message = "DemoParametersOne Response" });
    }

    public override Task<ParamResponse> DemoParametersTwo(RequestOne requestId, ServerCallContext ctx)
    {
        return Task.FromResult(new ParamResponse { Message = "DemoParametersTwo Response" });
    }

    public override Task<ParamResponse> DemoParametersThree(RequestTwo request, ServerCallContext ctx)
    {
        return Task.FromResult(new ParamResponse { Message = "DemoParametersThree Response " });
    }

    public override Task<ParamResponse> DemoParametersFour(RequestTwo request, ServerCallContext ctx)
    {
        return Task.FromResult(new ParamResponse { Message = "DemoParametersFour Response" });
    }
}
