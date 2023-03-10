// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;

internal static class EndpointJsonResponseEmitter
{
    internal static void EmitJsonPreparation(this Endpoint endpoint, CodeWriter codeWriter)
    {
        if (endpoint.Response.IsSerializable)
        {
            var typeName = endpoint.Response.ResponseType.ToDisplayString(EmitterConstants.DisplayFormat);

            codeWriter.WriteLine("var serviceProvider = options?.ServiceProvider ?? options?.EndpointBuilder?.ApplicationServices;");
            codeWriter.WriteLine("var serializerOptions = serviceProvider?.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions ?? new JsonOptions().SerializerOptions;");
            codeWriter.WriteLine($"var jsonTypeInfo =  (JsonTypeInfo<{typeName}>)serializerOptions.GetTypeInfo(typeof({typeName}));");
        }
    }

    internal static string EmitJsonResponse(this Endpoint endpoint)
    {
        if (endpoint.Response.ResponseType.IsSealed || endpoint.Response.ResponseType.IsValueType)
        {
            return $"httpContext.Response.WriteAsJsonAsync(result, jsonTypeInfo);";
        }
        else
        {
            return $"GeneratedRouteBuilderExtensionsCore.WriteToResponseAsync(result, httpContext, jsonTypeInfo, serializerOptions);";
        }
    }
}
