// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel.Emitters;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal class EndpointResponse
{
    public ITypeSymbol? ResponseType { get; set; }
    public string WrappedResponseType { get; set; }
    public string? ContentType { get; set; }
    public bool IsAwaitable { get; set; }
    public bool HasNoResponse { get; set; }
    public bool IsIResult { get; set; }
    public bool IsSerializable { get; set; }
    public bool IsEndpointMetadataProvider { get; set; }

    internal EndpointResponse(IMethodSymbol method)
    {
        ResponseType = UnwrapResponseType(method, out bool isAwaitable, out bool awaitableIsVoid);
        WrappedResponseType = method.ReturnType.ToDisplayString(EmitterConstants.DisplayFormat);
        IsAwaitable = isAwaitable;
        HasNoResponse = method.ReturnsVoid || awaitableIsVoid;
        IsIResult = GetIsIResult();
        IsSerializable = GetIsSerializable();
        ContentType = GetContentType();
        IsEndpointMetadataProvider = ImplementsIEndpointMetadataProvider(ResponseType);
    }

    private static bool ImplementsIEndpointMetadataProvider(ITypeSymbol? responseType)
        => responseType == null ? false : responseType.Implements(["Microsoft", "AspNetCore", "Http", "Metadata", "IEndpointMetadataProvider"]);

    private ITypeSymbol? UnwrapResponseType(IMethodSymbol method, out bool isAwaitable, out bool awaitableIsVoid)
    {
        isAwaitable = false;
        awaitableIsVoid = false;
        var returnType = method.ReturnType;
        if (returnType.OriginalDefinition.EqualsByName(["System", "Threading", "Tasks", "Task"]) ||
            returnType.OriginalDefinition.EqualsByName(["System", "Threading", "Tasks", "ValueTask"]))
        {
            isAwaitable = true;
            awaitableIsVoid = returnType is INamedTypeSymbol { IsGenericType: false };
            return returnType is INamedTypeSymbol { IsGenericType: true } namedReturnType
                ? namedReturnType.TypeArguments[0]
                : null;
        }

        return returnType;
    }

    private bool GetIsSerializable() =>
        !IsIResult &&
        !HasNoResponse &&
        ResponseType != null &&
        ResponseType.SpecialType != SpecialType.System_String &&
        ResponseType.SpecialType != SpecialType.System_Object;

    private bool GetIsIResult()
    {
        return ResponseType is not null &&
            (ResponseType.Implements(["Microsoft", "AspNetCore", "Http", "IResult"]) || ResponseType.EqualsByName(["Microsoft", "AspNetCore", "Http", "IResult"]));
    }

    private string? GetContentType()
    {
        // `void` returning methods do not have a Content-Type.
        // We don't have a strategy for resolving a Content-Type
        // from an IResult. Typically, this would be done via an
        // IEndpointMetadataProvider so we don't need to set a
        // Content-Type here.
        if (IsIResult || HasNoResponse)
        {
            return null;
        }

        return ResponseType!.SpecialType is SpecialType.System_String ? "text/plain; charset=utf-8" : "application/json";
    }

    public override bool Equals(object obj)
    {
        return obj is EndpointResponse otherEndpointResponse &&
            SymbolEqualityComparer.Default.Equals(otherEndpointResponse.ResponseType, ResponseType) &&
            otherEndpointResponse.WrappedResponseType.Equals(WrappedResponseType, StringComparison.Ordinal) &&
            otherEndpointResponse.IsAwaitable == IsAwaitable &&
            otherEndpointResponse.HasNoResponse == HasNoResponse &&
            otherEndpointResponse.IsIResult == IsIResult &&
            string.Equals(otherEndpointResponse.ContentType, ContentType, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() =>
        HashCode.Combine(SymbolEqualityComparer.Default.GetHashCode(ResponseType), WrappedResponseType, IsAwaitable, HasNoResponse, IsIResult, ContentType);
}
