// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;
internal static class EndpointEmitter
{
    internal static string EmitParameterPreparation(this Endpoint endpoint)
    {
        var parameterPreparationBuilder = new StringBuilder();

        for (var parameterIndex = 0; parameterIndex < endpoint.Parameters.Length; parameterIndex++)
        {
            var parameter = endpoint.Parameters[parameterIndex];

            var parameterPreparationCode = parameter switch
            {
                {
                    Source: EndpointParameterSource.SpecialType
                } => parameter.EmitSpecialParameterPreparation(),
                {
                    Source: EndpointParameterSource.Query,
                } => parameter.EmitQueryParameterPreparation(),
                {
                    Source: EndpointParameterSource.JsonBody
                } => parameter.EmitJsonBodyParameterPreparationString(),
                {
                    Source: EndpointParameterSource.Service
                } => parameter.EmitServiceParameterPreparation(),
                _ => throw new Exception("Unreachable!")
            };

            // To avoid having two newlines after the block of parameter handling code.
            if (parameterIndex < endpoint.Parameters.Length - 1)
            {
                parameterPreparationBuilder.AppendLine(parameterPreparationCode);
            }
            else
            {
                parameterPreparationBuilder.Append(parameterPreparationCode);
            }
        }

        return parameterPreparationBuilder.ToString();
    }
}
