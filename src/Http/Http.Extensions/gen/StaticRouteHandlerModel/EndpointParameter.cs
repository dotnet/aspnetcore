// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
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
        HandlerArgument = $"{parameter.Name}_local";
        AssigningCodeResult = $"{parameter.Name}_raw";

        var fromQueryMetadataInterfaceType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromQueryMetadata);
        var fromServiceMetadataInterfaceType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata);
        var fromRouteMetadataInterfaceType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromRouteMetadata);
        var fromHeaderMetadataInterfaceType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromHeaderMetadata);

        if (parameter.HasAttributeImplementingInterface(fromRouteMetadataInterfaceType, out var fromRouteAttribute))
        {
            Source = EndpointParameterSource.Route;
            Name = fromRouteAttribute.TryGetNamedArgumentValue<string>("Name", out var fromRouteName)
                ? fromRouteName
                : parameter.Name;
            AssigningCode = $"httpContext.Request.RouteValues[\"{Name}\"]?.ToString()";
            IsOptional = parameter.IsOptional();
        }
        else if (parameter.HasAttributeImplementingInterface(fromQueryMetadataInterfaceType, out var fromQueryAttribute))
        {
            Source = EndpointParameterSource.Query;
            Name = fromQueryAttribute.TryGetNamedArgumentValue<string>("Name", out var fromQueryName)
                ? fromQueryName
                : parameter.Name;
            AssigningCode = $"httpContext.Request.Query[\"{Name}\"]";
            IsOptional = parameter.IsOptional();
        }
        else if (parameter.HasAttributeImplementingInterface(fromHeaderMetadataInterfaceType, out var fromHeaderAttribute))
        {
            Source = EndpointParameterSource.Header;
            Name = fromHeaderAttribute.TryGetNamedArgumentValue<string>("Name", out var fromHeaderName)
                ? fromHeaderName
                : parameter.Name;
            AssigningCode = $"httpContext.Request.Headers[\"{Name}\"]";
            IsOptional = parameter.IsOptional();
        }
        else if (TryGetExplicitFromJsonBody(parameter, wellKnownTypes, out var jsonBodyAssigningCode, out var isOptional))
        {
            Source = EndpointParameterSource.JsonBody;
            AssigningCode = jsonBodyAssigningCode;
            IsOptional = isOptional;
        }
        else if (parameter.HasAttributeImplementingInterface(fromServiceMetadataInterfaceType))
        {
            Source = EndpointParameterSource.Service;
            IsOptional = parameter.Type is INamedTypeSymbol { NullableAnnotation: NullableAnnotation.Annotated } || parameter.HasExplicitDefaultValue;
            AssigningCode = IsOptional ?
                $"httpContext.RequestServices.GetService<{parameter.Type}>();" :
                $"httpContext.RequestServices.GetRequiredService<{parameter.Type}>()";
        }
        else if (TryGetSpecialTypeAssigningCode(Type, wellKnownTypes, out var specialTypeAssigningCode))
        {
            Source = EndpointParameterSource.SpecialType;
            AssigningCode = specialTypeAssigningCode;
        }
        else if (parameter.Type.SpecialType == SpecialType.System_String)
        {
            Source = EndpointParameterSource.RouteOrQuery;
            AssigningCode = $"GeneratedRouteBuilderExtensionsCore.ResolveFromRouteOrQuery(httpContext, \"{parameter.Name}\", options?.RouteParameterNames)";
            IsOptional = parameter.IsOptional();
        }
        else
        {
            // TODO: Inferencing rules go here - but for now:
            Source = EndpointParameterSource.Unknown;
        }
    }

    public ITypeSymbol Type { get; }
    public EndpointParameterSource Source { get; }

    // TODO: If the parameter has [FromRoute("AnotherName")] or similar, prefer that.
    public string Name { get; }
    public string? AssigningCode { get; }
    public string HandlerArgument { get; }

    public string AssigningCodeResult { get; }
    public bool IsOptional { get; }

    public string EmitArgument() => Source switch
    {
        EndpointParameterSource.JsonBody or EndpointParameterSource.Route or EndpointParameterSource.RouteOrQuery => IsOptional ? HandlerArgument : $"{HandlerArgument}!",
        EndpointParameterSource.Unknown => throw new Exception("Unreachable!"),
        _ => HandlerArgument
    };

    // TODO: Handle special form types like IFormFileCollection that need special body-reading logic.
    private static bool TryGetSpecialTypeAssigningCode(ITypeSymbol type, WellKnownTypes wellKnownTypes, [NotNullWhen(true)] out string? callingCode)
    {
        callingCode = null;
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_HttpContext)))
        {
            callingCode = "httpContext";
            return true;
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_HttpRequest)))
        {
            callingCode = "httpContext.Request";
            return true;
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_HttpResponse)))
        {
            callingCode = "httpContext.Response";
            return true;
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.System_IO_Pipelines_PipeReader)))
        {
            callingCode = "httpContext.Request.BodyReader";
            return true;
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.System_IO_Stream)))
        {
            callingCode = "httpContext.Request.Body";
            return true;
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.System_Security_Claims_ClaimsPrincipal)))
        {
            callingCode = "httpContext.User";
            return true;
        }
        if (SymbolEqualityComparer.Default.Equals(type, wellKnownTypes.Get(WellKnownType.System_Threading_CancellationToken)))
        {
            callingCode = "httpContext.RequestAborted";
            return true;
        }

        return false;
    }

    private static bool TryGetExplicitFromJsonBody(IParameterSymbol parameter,
        WellKnownTypes wellKnownTypes,
        [NotNullWhen(true)] out string? assigningCode,
        out bool isOptional)
    {
        assigningCode = null;
        isOptional = false;
        if (parameter.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromBodyMetadata), out var fromBodyAttribute))
        {
            isOptional |= fromBodyAttribute.TryGetNamedArgumentValue<bool>("AllowEmpty", out var allowEmptyValue) && allowEmptyValue;
            isOptional |= (parameter.NullableAnnotation == NullableAnnotation.Annotated || parameter.HasExplicitDefaultValue);
            assigningCode = $"await GeneratedRouteBuilderExtensionsCore.TryResolveBody<{parameter.Type}>(httpContext, {(isOptional ? "true" : "false")})";
            return true;
        }
        return false;
    }

    public override bool Equals(object obj) =>
        obj is EndpointParameter other &&
        other.Source == Source &&
        other.Name == Name &&
        SymbolEqualityComparer.Default.Equals(other.Type, Type);

    public bool SignatureEquals(object obj) =>
        obj is EndpointParameter other &&
        SymbolEqualityComparer.Default.Equals(other.Type, Type);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Name);
        hashCode.Add(Type, SymbolEqualityComparer.Default);
        return hashCode.ToHashCode();
    }
}
