// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel.Emitters;

internal static class EndpointJsonPreparationEmitter
{
    internal static void EmitJsonPreparation(this Endpoint endpoint, CodeWriter codeWriter)
    {
        codeWriter.WriteLine("var jsonOptions = serviceProvider?.GetService<IOptions<JsonOptions>>()?.Value ?? FallbackJsonOptions;");
        codeWriter.WriteLine("var jsonSerializerOptions = jsonOptions.SerializerOptions;");
        codeWriter.WriteLine("jsonSerializerOptions.MakeReadOnly();");
        codeWriter.WriteLine($"var objectJsonTypeInfo = (JsonTypeInfo<object?>)jsonSerializerOptions.GetTypeInfo(typeof(object));");

        if (endpoint.Response?.IsSerializableJsonResponse(out var responseType) == true)
        {
            var typeName = responseType.ToDisplayString(EmitterConstants.DisplayFormatWithoutNullability);
            codeWriter.WriteLine($"var responseJsonTypeInfo =  (JsonTypeInfo<{responseType.ToDisplayString(NullableFlowState.MaybeNull, EmitterConstants.DisplayFormat)}>)jsonSerializerOptions.GetTypeInfo(typeof({typeName}));");
        }

        foreach (var parameter in endpoint.Parameters)
        {
            ProcessParameter(parameter, codeWriter);
            if (parameter is { Source: EndpointParameterSource.AsParameters, EndpointParameters: {} innerParameters })
            {
                foreach (var innerParameter in innerParameters)
                {
                    ProcessParameter(innerParameter, codeWriter);
                }
            }
        }

        static void ProcessParameter(EndpointParameter parameter, CodeWriter codeWriter)
        {
            if (parameter.Source != EndpointParameterSource.JsonBody)
            {
                return;
            }
            var typeName = parameter.Type.ToDisplayString(EmitterConstants.DisplayFormat);
            codeWriter.WriteLine($"var {parameter.SymbolName}_JsonTypeInfo = (JsonTypeInfo<{typeName}>)jsonSerializerOptions.GetTypeInfo(typeof({parameter.Type.ToDisplayString(EmitterConstants.DisplayFormatWithoutNullability)}));");
        }

    }

    internal static string EmitJsonResponse(this EndpointResponse endpointResponse)
    {
        if (endpointResponse.ResponseType != null &&
            (endpointResponse.ResponseType.IsSealed || endpointResponse.ResponseType.IsValueType))
        {
            return "httpContext.Response.WriteAsJsonAsync(result, responseJsonTypeInfo);";
        }
        return "GeneratedRouteBuilderExtensionsCore.WriteJsonResponseAsync(httpContext.Response, result, responseJsonTypeInfo);";
    }
}
