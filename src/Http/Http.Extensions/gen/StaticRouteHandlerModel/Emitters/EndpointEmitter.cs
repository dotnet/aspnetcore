// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;
internal static class EndpointEmitter
{
    internal static string EmitParameterPreparation(this Endpoint endpoint, int baseIndent = 0)
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var parameterPreparationBuilder = new CodeWriter(stringWriter, baseIndent);

        foreach (var parameter in endpoint.Parameters)
        {
            switch (parameter.Source)
            {
                case EndpointParameterSource.SpecialType:
                    parameter.EmitSpecialParameterPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.Query:
                case EndpointParameterSource.Header:
                    parameter.EmitQueryOrHeaderParameterPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.Route:
                    parameter.EmitRouteParameterPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.RouteOrQuery:
                    parameter.EmitRouteOrQueryParameterPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.BindAsync:
                    parameter.EmitBindAsyncPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.JsonBody:
                    parameter.EmitJsonBodyParameterPreparationString(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.JsonBodyOrService:
                    parameter.EmitJsonBodyOrServiceParameterPreparationString(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.Service:
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
                var parameterName = parameter.SymbolName;
                codeWriter.Write($@"var {parameterName}_RouteOrQueryResolver = ");
                codeWriter.WriteLine($@"GeneratedRouteBuilderExtensionsCore.ResolveFromRouteOrQuery(""{parameterName}"", options?.RouteParameterNames);");
            }
        }
    }

    public static void EmitJsonBodyOrServiceResolver(this Endpoint endpoint, CodeWriter codeWriter)
    {
        var serviceProviderEmitted = false;
        foreach (var parameter in endpoint.Parameters)
        {
            if (parameter.Source == EndpointParameterSource.JsonBodyOrService)
            {
                if (!serviceProviderEmitted)
                {
                    codeWriter.WriteLine("var serviceProviderIsService = options?.ServiceProvider?.GetService<IServiceProviderIsService>();");
                    serviceProviderEmitted = true;
                }
                codeWriter.Write($@"var {parameter.Name}_JsonBodyOrServiceResolver = ");
                codeWriter.WriteLine($"ResolveJsonBodyOrService<{parameter.Type.ToDisplayString(EmitterConstants.DisplayFormat)}>(serviceProviderIsService);");
            }
        }
    }

    public static string EmitArgumentList(this Endpoint endpoint) => string.Join(", ", endpoint.Parameters.Select(p => p.EmitArgument()));
}
