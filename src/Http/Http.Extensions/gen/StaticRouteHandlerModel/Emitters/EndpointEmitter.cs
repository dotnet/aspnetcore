// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel.Emitters;
internal static class EndpointEmitter
{
    internal static string EmitParameterPreparation(this IEnumerable<EndpointParameter> endpointParameters, int baseIndent = 0)
    {
        using var stringWriter = new StringWriter(CultureInfo.InvariantCulture);
        using var parameterPreparationBuilder = new CodeWriter(stringWriter, baseIndent);
        var readFormEmitted = false;

        foreach (var parameter in endpointParameters)
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
                case EndpointParameterSource.FormBody:
                    parameter.EmitFormParameterPreparation(parameterPreparationBuilder, ref readFormEmitted);
                    break;
                case EndpointParameterSource.JsonBodyOrService:
                    parameter.EmitJsonBodyOrServiceParameterPreparationString(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.Service:
                    parameter.EmitServiceParameterPreparation(parameterPreparationBuilder);
                    break;
                case EndpointParameterSource.AsParameters:
                    parameter.EmitAsParametersParameterPreparation(parameterPreparationBuilder);
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
            if (parameter is
            {
                Source: EndpointParameterSource.AsParameters,
                EndpointParameters:
                {} asParametersList
            })
            {
                foreach (var (_, innerParameter) in asParametersList)
                {
                    if (innerParameter.Source == EndpointParameterSource.RouteOrQuery)
                    {
                        var parameterName = innerParameter.SymbolName;
                        codeWriter.Write($@"var {parameterName}_RouteOrQueryResolver = ");
                        codeWriter.WriteLine($@"GeneratedRouteBuilderExtensionsCore.ResolveFromRouteOrQuery(""{parameterName}"", options?.RouteParameterNames);");
                        endpoint.EmitterContext.HasRouteOrQuery = true;
                    }
                }
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
                    codeWriter.WriteLine("var serviceProviderIsService = serviceProvider?.GetService<IServiceProviderIsService>();");
                    serviceProviderEmitted = true;
                }
                codeWriter.Write($@"var {parameter.SymbolName}_JsonBodyOrServiceResolver = ");
                var shortParameterTypeName = parameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
                codeWriter.WriteLine($"ResolveJsonBodyOrService<{parameter.Type.ToDisplayString(EmitterConstants.DisplayFormat)}>(logOrThrowExceptionHelper, {SymbolDisplay.FormatLiteral(shortParameterTypeName, true)}, {SymbolDisplay.FormatLiteral(parameter.SymbolName, true)}, serviceProviderIsService);");
            }
        }
    }

    public static void EmitLoggingPreamble(this Endpoint endpoint, CodeWriter codeWriter)
    {
        if (endpoint.EmitterContext.RequiresLoggingHelper)
        {
            codeWriter.WriteLine("var logOrThrowExceptionHelper = new LogOrThrowExceptionHelper(serviceProvider, options);");
        }
    }

    public static string EmitArgumentList(this Endpoint endpoint) => string.Join(", ", endpoint.Parameters.Select(p => p.EmitArgument()));
}
