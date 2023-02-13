// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;
internal static class EndpointParameterEmitter
{
    internal static string EmitSpecialParameterPreparation(this EndpointParameter endpointParameter)
    {
        return $"""
                var {endpointParameter.Name}_local = {endpointParameter.AssigningCode};
                """;
    }

    internal static string EmitQueryParameterPreparation(this EndpointParameter endpointParameter)
    {
        return $$"""
                 var {{endpointParameter.Name}}_raw = {{endpointParameter.AssigningCode}};

                 if (StringValues.IsNullOrEmpty({{endpointParameter.Name}}_raw) && {{(endpointParameter.IsOptional ? "false" : "true")}})
                 {
                     httpContext.Response.StatusCode = 400;
                     return Task.CompletedTask;
                 }

                 var {{endpointParameter.HandlerArgument}} = {{endpointParameter.Name}}_raw.ToString();
                 """;
    }
}
