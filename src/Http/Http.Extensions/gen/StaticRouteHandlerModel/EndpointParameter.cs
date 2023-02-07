// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using WellKnownType = Microsoft.AspNetCore.App.Analyzers.Infrastructure.WellKnownTypeData.WellKnownType;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

internal class EndpointParameter
{
    public EndpointParameter(IParameterSymbol parameter, WellKnownTypes wellKnownTypes)
    {
        Type = parameter.Type;
        Name = parameter.Name;
        Source = EndpointParameterSource.Unknown;

        if (GetSpecialTypeCallingCode(Type, wellKnownTypes) is string callingCode)
        {
            Source = EndpointParameterSource.SpecialType;
            CallingCode = callingCode;
        }
    }

    public ITypeSymbol Type { get; }
    public EndpointParameterSource Source { get; }

    // TODO: If the parameter has [FromRoute("AnotherName")] or similar, prefer that.
    public string Name { get; }
    public string? CallingCode { get; }

    public string EmitArgument()
    {
        switch (Source)
        {
            case EndpointParameterSource.SpecialType:
                return CallingCode!;
            default:
                // Eventually there should be know unknown parameter sources, but in the meantime we don't expect them to get this far.
                // The netstandard2.0 target means there is no UnreachableException.
                throw new Exception("Unreachable!");
        }
    }

    // TODO: Handle special form types like IFormFileCollection that need special body-reading logic.
    private static string? GetSpecialTypeCallingCode(ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_HttpContext)))
        {
            return "httpContext";
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_HttpRequest)))
        {
            return "httpContext.Request";
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_HttpResponse)))
        {
            return "httpContext.Response";
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.System_IO_Pipelines_PipeReader)))
        {
            return "httpContext.Request.BodyReader";
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.System_IO_Stream)))
        {
            return "httpContext.Request.Body";
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.System_Security_Claims_ClaimsPrincipal)))
        {
            return "httpContext.User";
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.System_Threading_CancellationToken)))
        {
            return "httpContext.RequestAborted";
        }

        return null;
    }

    public override bool Equals(object obj) =>
        obj is EndpointParameter other &&
        other.Source == Source &&
        other.Name == Name &&
        SymbolEqualityComparer.Default.Equals(other.Type, Type);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Name);
        hashCode.Add(Type, SymbolEqualityComparer.Default);
        return hashCode.ToHashCode();
    }
}
