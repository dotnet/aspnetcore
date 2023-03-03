// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;
internal static class EndpointEmitter
{
    internal static string EmitParameterPreparation(this Endpoint endpoint, int baseIndent = 0)
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var parameterPreparationBuilder = new CodeWriter(stringWriter, baseIndent);

        foreach (var parameter in endpoint.Parameters)
        {
            switch (parameter)
            {
                case { Source: EndpointParameterSource.SpecialType }:
                    parameter.EmitSpecialParameterPreparation(parameterPreparationBuilder);
                    break;
                case { Source: EndpointParameterSource.Query or EndpointParameterSource.Header }:
                    parameter.EmitQueryOrHeaderParameterPreparation(parameterPreparationBuilder);
                    break;
                case { Source: EndpointParameterSource.Route }:
                    parameter.EmitRouteParameterPreparation(parameterPreparationBuilder);
                    break;
                case { Source: EndpointParameterSource.RouteOrQuery }:
                    parameter.EmitRouteOrQueryParameterPreparation(parameterPreparationBuilder);
                    break;
                case { Source: EndpointParameterSource.JsonBody }:
                    parameter.EmitJsonBodyParameterPreparationString(parameterPreparationBuilder);
                    break;
                case { Source: EndpointParameterSource.Service }:
                    parameter.EmitServiceParameterPreparation(parameterPreparationBuilder);
                    break;
            }
        }

        return stringWriter.ToString();
    }

    public static void EmitRouteOrQueryResolver(this Endpoint endpoint, CodeWriter codeWriter)
    {
        foreach (var parameter in endpoint.Parameters)
        {
            if (parameter.Source == EndpointParameterSource.RouteOrQuery)
            {
                var parameterName = parameter.Name;
                codeWriter.Write($@"var {parameterName}_RouteOrQueryResolver = ");
                codeWriter.WriteLine($@"GeneratedRouteBuilderExtensionsCore.ResolveFromRouteOrQuery(""{parameterName}"", options?.RouteParameterNames);");
            }
        }
    }

    public static string EmitArgumentList(this Endpoint endpoint) => string.Join(", ", endpoint.Parameters.Select(p => p.EmitArgument()));
}
