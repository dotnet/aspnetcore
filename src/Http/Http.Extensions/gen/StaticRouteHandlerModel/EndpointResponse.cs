// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel.Emitters;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

using WellKnownType = WellKnownTypeData.WellKnownType;

internal class EndpointResponse
{
    public ITypeSymbol? ResponseType { get; set; }
    public string WrappedResponseType { get; set; }
    public string ContentType { get; set; }
    public bool IsAwaitable { get; set; }
    public bool IsVoid { get; set; }
    public bool IsIResult { get; set; }

    private WellKnownTypes WellKnownTypes { get; init; }

    internal EndpointResponse(IMethodSymbol method, WellKnownTypes wellKnownTypes)
    {
        WellKnownTypes = wellKnownTypes;
        ResponseType = UnwrapResponseType(method);
        WrappedResponseType = method.ReturnType.ToDisplayString(EmitterConstants.DisplayFormat);
        IsAwaitable = GetIsAwaitable(method);
        IsVoid = method.ReturnsVoid;
        IsIResult = GetIsIResult();
        ContentType = GetContentType(method);
    }

    private ITypeSymbol UnwrapResponseType(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        var task = WellKnownTypes.Get(WellKnownType.System_Threading_Tasks_Task);
        var taskOfT = WellKnownTypes.Get(WellKnownType.System_Threading_Tasks_Task_T);
        var valueTask = WellKnownTypes.Get(WellKnownType.System_Threading_Tasks_ValueTask);
        var valueTaskOfT = WellKnownTypes.Get(WellKnownType.System_Threading_Tasks_ValueTask_T);
        if (returnType.OriginalDefinition.Equals(taskOfT, SymbolEqualityComparer.Default) ||
            returnType.OriginalDefinition.Equals(valueTaskOfT, SymbolEqualityComparer.Default))
        {
            return ((INamedTypeSymbol)returnType).TypeArguments[0];
        }

        if (returnType.OriginalDefinition.Equals(task, SymbolEqualityComparer.Default) ||
            returnType.OriginalDefinition.Equals(valueTask, SymbolEqualityComparer.Default))
        {
            return null;
        }

        return returnType;
    }

    private bool GetIsIResult()
    {
        var resultType = WellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IResult);
        return WellKnownTypes.Implements(ResponseType, resultType) ||
            SymbolEqualityComparer.Default.Equals(ResponseType, resultType);
    }

    private static bool GetIsAwaitable(IMethodSymbol method)
    {
        var potentialGetAwaiters = method.ReturnType.OriginalDefinition.GetMembers(WellKnownMemberNames.GetAwaiter);
        var getAwaiters = potentialGetAwaiters.OfType<IMethodSymbol>().Where(x => !x.Parameters.Any());
        return getAwaiters.Any(symbol => symbol.Name == WellKnownMemberNames.GetAwaiter && VerifyGetAwaiter(symbol));

        static bool VerifyGetAwaiter(IMethodSymbol getAwaiter)
        {
            var returnType = getAwaiter.ReturnType;

            // bool IsCompleted { get }
            if (!returnType.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(p => p.Name == WellKnownMemberNames.IsCompleted &&
                    p.Type.SpecialType == SpecialType.System_Boolean && p.GetMethod != null))
            {
                return false;
            }

            var methods = returnType.GetMembers().OfType<IMethodSymbol>();

            if (!methods.Any(x => x.Name == WellKnownMemberNames.OnCompleted &&
                x.ReturnsVoid &&
                x.Parameters.Length == 1 &&
                x.Parameters.First().Type.TypeKind == TypeKind.Delegate))
            {
                return false;
            }

            // void GetResult() || T GetResult()
            return methods.Any(m => m.Name == WellKnownMemberNames.GetResult && !m.Parameters.Any());
        }
    }

    private string? GetContentType(IMethodSymbol method)
    {
        // `void` returning methods do not have a Content-Type.
        // We don't have a strategy for resolving a Content-Type
        // from an IResult. Typically, this would be done via an
        // IEndpointMetadataProvider so we don't need to set a
        // Content-Type here.
        if (method.ReturnsVoid || IsIResult)
        {
            return null;
        }
        return method.ReturnType.SpecialType is SpecialType.System_String ? "text/plain" : "application/json";
    }

    public override bool Equals(object obj)
    {
        return obj is EndpointResponse otherEndpointResponse &&
            SymbolEqualityComparer.Default.Equals(otherEndpointResponse.ResponseType, ResponseType) &&
            otherEndpointResponse.WrappedResponseType.Equals(WrappedResponseType, StringComparison.Ordinal) &&
            otherEndpointResponse.IsAwaitable == IsAwaitable &&
            otherEndpointResponse.IsVoid == IsVoid &&
            otherEndpointResponse.IsIResult == IsIResult &&
            otherEndpointResponse.ContentType.Equals(ContentType, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() =>
        HashCode.Combine(SymbolEqualityComparer.Default.GetHashCode(ResponseType), WrappedResponseType, IsAwaitable, IsVoid, IsIResult, ContentType);
}
