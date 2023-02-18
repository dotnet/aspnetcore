// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;

internal static class EndpointJsonResponseEmitter
{
    internal static string EmitJsonPreparation(this Endpoint endpoint)
    {
        var jsonPreparation = new StringBuilder();

        if (endpoint.Response.IsSerializable)
        {
            var typeName = endpoint.Response.ResponseType.ToDisplayString(EmitterConstants.DisplayFormat);

            jsonPreparation.AppendLine($"""

                    var serializerOptions = serviceProvider?.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions ?? new JsonOptions().SerializerOptions;
                    var jsonTypeInfo =  (JsonTypeInfo<{typeName}>)serializerOptions.GetTypeInfo(typeof({typeName}));
""");
        }

        return jsonPreparation.ToString();
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
