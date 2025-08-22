// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal static class EmitterExtensions
{
    public static string ToMessageString(this EndpointParameter endpointParameter) => endpointParameter.Source switch
    {
        EndpointParameterSource.Header => "header",
        EndpointParameterSource.Query => "query string",
        EndpointParameterSource.Route => "route",
        EndpointParameterSource.RouteOrQuery => "route or query string",
        EndpointParameterSource.FormBody => "form",
        EndpointParameterSource.BindAsync => endpointParameter.BindMethod == BindabilityMethod.BindAsync
            ? $"{endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)}.BindAsync(HttpContext)"
            : $"{endpointParameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)}.BindAsync(HttpContext, ParameterInfo)",
        _ => "unknown"
    };

    public static bool IsSerializableJsonResponse(this EndpointResponse endpointResponse, [NotNullWhen(true)] out ITypeSymbol? responseTypeSymbol)
    {
        responseTypeSymbol = null;
        if (endpointResponse is { IsSerializable: true, ResponseType: { } responseType })
        {
            responseTypeSymbol = responseType;
            return true;
        }
        return false;
    }

    public static string EmitHandlerArgument(this EndpointParameter endpointParameter) => $"{endpointParameter.SymbolName}_local";

    public static string EmitArgument(this EndpointParameter endpointParameter) => endpointParameter.Source switch
    {
        EndpointParameterSource.JsonBody or EndpointParameterSource.Route or EndpointParameterSource.RouteOrQuery or EndpointParameterSource.JsonBodyOrService or EndpointParameterSource.FormBody => endpointParameter.IsOptional ? endpointParameter.EmitHandlerArgument() : $"{endpointParameter.EmitHandlerArgument()}!",
        // When a BindAsync parameter is required, make sure that we are using `.Value` to access
        // the underlying value for a nullable value type instead of using the non-nullable reference type modifier.
        EndpointParameterSource.BindAsync => endpointParameter.IsOptional ?
            endpointParameter.EmitHandlerArgument() :
            endpointParameter.Type.IsValueType && endpointParameter.GetBindAsyncReturnType().IsNullableOfT()
                ? $"{endpointParameter.EmitHandlerArgument()}.HasValue ? {endpointParameter.EmitHandlerArgument()}.Value : default"
                : $"{endpointParameter.EmitHandlerArgument()}",
        EndpointParameterSource.Unknown => throw new NotImplementedException("Unreachable!"),
        _ => endpointParameter.EmitHandlerArgument()
    };
}
