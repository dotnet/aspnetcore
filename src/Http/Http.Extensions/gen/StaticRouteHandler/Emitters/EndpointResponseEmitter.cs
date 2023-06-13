// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandler.Model;
using Microsoft.CodeAnalysis;
namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandler.Emitters;

internal static class EndpointResponseEmitter
{
    internal static void EmitJsonPreparation(this EndpointResponse endpointResponse, CodeWriter codeWriter)
    {
        if (endpointResponse.IsSerializableJsonResponse(out var responseType))
        {
            var typeName = responseType.ToDisplayString(EmitterConstants.DisplayFormatWithoutNullability);

            codeWriter.WriteLine("var serializerOptions = serviceProvider?.GetService<IOptions<JsonOptions>>()?.Value.SerializerOptions ?? new JsonOptions().SerializerOptions;");
            codeWriter.WriteLine($"var jsonTypeInfo =  (JsonTypeInfo<{typeName}>)serializerOptions.GetTypeInfo(typeof({typeName}));");
        }
    }

    private static string EmitJsonResponse(this EndpointResponse endpointResponse)
    {
        if (endpointResponse.ResponseType != null &&
            (endpointResponse.ResponseType.IsSealed || endpointResponse.ResponseType.IsValueType))
        {
            return $"httpContext.Response.WriteAsJsonAsync(result, jsonTypeInfo);";
        }
        return $"GeneratedRouteBuilderExtensionsCore.WriteToResponseAsync(result, httpContext, jsonTypeInfo);";
    }

    internal static string EmitResponseWritingCall(this EndpointResponse endpointResponse, bool isAwaitable)
    {
        var returnOrAwait = isAwaitable ? "await" : "return";

        if (endpointResponse.IsIResult)
        {
            return $"{returnOrAwait} GeneratedRouteBuilderExtensionsCore.ExecuteAsyncExplicit(result, httpContext);";
        }
        if (endpointResponse.ResponseType?.SpecialType == SpecialType.System_String)
        {
            return $"{returnOrAwait} httpContext.Response.WriteAsync(result);";
        }
        if (endpointResponse.ResponseType?.SpecialType == SpecialType.System_Object)
        {
            return $"{returnOrAwait} GeneratedRouteBuilderExtensionsCore.ExecuteObjectResult(result, httpContext);";
        }
        if (!endpointResponse.HasNoResponse)
        {
            return $"{returnOrAwait} {endpointResponse.EmitJsonResponse()}";
        }
        if (!endpointResponse.IsAwaitable && endpointResponse.HasNoResponse)
        {
            return $"{returnOrAwait} Task.CompletedTask;";
        }
        return $"{returnOrAwait} httpContext.Response.WriteAsync(result);";
    }

    internal static void EmitBuiltinResponseTypeMetadata(this Endpoint endpoint, CodeWriter codeWriter)
    {
        if (endpoint.Response is not { } response || response.ResponseType is not { } responseType)
        {
            return;
        }

        if (response.HasNoResponse || response.IsIResult)
        {
            return;
        }

        if (responseType.SpecialType == SpecialType.System_String)
        {
            codeWriter.WriteLine("options.EndpointBuilder.Metadata.Add(new GeneratedProducesResponseTypeMetadata(type: null, statusCode: StatusCodes.Status200OK, contentTypes: GeneratedMetadataConstants.PlaintextContentType));");
        }
        else
        {
            codeWriter.WriteLine($"options.EndpointBuilder.Metadata.Add(new GeneratedProducesResponseTypeMetadata(type: typeof({responseType.ToDisplayString(EmitterConstants.DisplayFormatWithoutNullability)}), statusCode: StatusCodes.Status200OK, contentTypes: GeneratedMetadataConstants.JsonContentType));");
        }
    }

    internal static void EmitCallToMetadataProviderForResponse(this Endpoint endpoint, CodeWriter codeWriter)
    {
        if (endpoint.Response is not { } response || response.ResponseType is not { } responseType)
        {
            return;
        }

        if (response.IsEndpointMetadataProvider)
        {
            codeWriter.WriteLine($"PopulateMetadataForEndpoint<{responseType.ToDisplayString(EmitterConstants.DisplayFormat)}>(methodInfo, options.EndpointBuilder);");
        }
    }

    internal static void EmitHttpResponseContentType(this EndpointResponse endpointResponse, CodeWriter codeWriter)
    {
        if (!endpointResponse.HasNoResponse
            && endpointResponse.ResponseType is { } responseType
            && (responseType.SpecialType == SpecialType.System_Object || responseType.SpecialType == SpecialType.System_String))
        {
            codeWriter.WriteLine("if (result is string)");
            codeWriter.StartBlock();
            codeWriter.WriteLine($@"httpContext.Response.ContentType ??= ""text/plain; charset=utf-8"";");
            codeWriter.EndBlock();
            codeWriter.WriteLine("else");
            codeWriter.StartBlock();
            codeWriter.WriteLine($@"httpContext.Response.ContentType ??= ""application/json; charset=utf-8"";");
            codeWriter.EndBlock();
        }
    }
}
