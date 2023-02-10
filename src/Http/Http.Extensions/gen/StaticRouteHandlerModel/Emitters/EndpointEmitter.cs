// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;
internal static class EndpointEmitter
{
    internal static string EmitParameterPreparation(this Endpoint endpoint)
    {
        var parameterPreparationBuilder = new StringBuilder();

        foreach (var parameter in endpoint.Parameters)
        {
            var parameterPreparationCode = parameter switch
            {
                {
                    Source: EndpointParameterSource.SpecialType
                } => parameter.EmitSpecialParameterPreparation(),
                {
                    Source: EndpointParameterSource.Query,
                } => parameter.EmitQueryParameterPreparation(),
                _ => throw new Exception("Unreachable!")
            };

            parameterPreparationBuilder.AppendLine(parameterPreparationCode);
        }

        return parameterPreparationBuilder.ToString();
    }
}
