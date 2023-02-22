// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.CodeAnalysis;
using WellKnownType = Microsoft.AspNetCore.App.Analyzers.Infrastructure.WellKnownTypeData.WellKnownType;
using Microsoft.AspNetCore.Analyzers.Infrastructure;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

internal class EndpointParameter
{
    public EndpointParameter(IParameterSymbol parameter, WellKnownTypes wellKnownTypes)
    {
        Type = parameter.Type;
        Name = parameter.Name;
        Source = EndpointParameterSource.Unknown;

        var fromQueryMetadataInterfaceType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromQueryMetadata);
        var fromServiceMetadataInterfaceType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata);
        var fromRouteMetadataInterfaceType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromRouteMetadata);
        var fromHeaderMetadataInterfaceType = wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromHeaderMetadata);

        if (parameter.HasAttributeImplementingInterface(fromRouteMetadataInterfaceType, out var fromRouteAttribute))
        {
            Source = EndpointParameterSource.Route;
            Name = GetParameterName(fromRouteAttribute, parameter.Name);
            IsOptional = parameter.IsOptional();
        }
        else if (parameter.HasAttributeImplementingInterface(fromQueryMetadataInterfaceType, out var fromQueryAttribute))
        {
            Source = EndpointParameterSource.Query;
            Name = GetParameterName(fromQueryAttribute, parameter.Name);
            IsOptional = parameter.IsOptional();
            AssigningCode = $"httpContext.Request.Query[\"{parameter.Name}\"]";
            IsParsable = TryGetParsability(parameter, wellKnownTypes, out var parsingBlockEmitter);
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else if (parameter.HasAttributeImplementingInterface(fromHeaderMetadataInterfaceType, out var fromHeaderAttribute))
        {
            Source = EndpointParameterSource.Header;
            Name = GetParameterName(fromHeaderAttribute, parameter.Name);
            IsOptional = parameter.IsOptional();
        }
        else if (TryGetExplicitFromJsonBody(parameter, wellKnownTypes, out var isOptional))
        {
            Source = EndpointParameterSource.JsonBody;
            IsOptional = isOptional;
        }
        else if (parameter.HasAttributeImplementingInterface(fromServiceMetadataInterfaceType))
        {
            Source = EndpointParameterSource.Service;
            IsOptional = parameter.Type is INamedTypeSymbol { NullableAnnotation: NullableAnnotation.Annotated } || parameter.HasExplicitDefaultValue;
        }
        else if (TryGetSpecialTypeAssigningCode(Type, wellKnownTypes, out var specialTypeAssigningCode))
        {
            Source = EndpointParameterSource.SpecialType;
            AssigningCode = specialTypeAssigningCode;
        }
        else if (parameter.Type.SpecialType == SpecialType.System_String)
        {
            Source = EndpointParameterSource.RouteOrQuery;
            IsOptional = parameter.IsOptional();
        }
        else
        {
            // TODO: Inferencing rules go here - but for now:
            Source = EndpointParameterSource.Unknown;
        }
    }

    private static bool TryGetParsability(IParameterSymbol parameter, WellKnownTypes wellKnownTypes, [NotNullWhen(true)]out Func<string, string, string>? parsingBlockEmitter)
    {
        // ParsabilityHelper returns a single enumeration with a Parsable/NonParsable enumeration result. We use this already
        // in the analyzers to determine whether we need to warn on whether a type needs to implement TryParse/IParsable<T>. To
        // support usage in the code generator an optiona out parameter has been added to hint at what variant of the various
        // TryParse methods should be used (this implies that the preferences are baked into ParsabilityHelper). If we aren't
        // parsable at all we bail.
        if (ParsabilityHelper.GetParsability(parameter.Type, wellKnownTypes, out var parsabilityMethod) == Parsability.NotParsable)
        {
            parsingBlockEmitter = null;
            return false;
        }

        // If we are parsable we need to emit code based on the enumeration ParsabilityMethod which has a bunch of members
        // which spell out the preferred TryParse uage. This swtich statement makes slight variations to them based on
        // which method was encountered.
        Func<string, string, string>? preferredTryParseInvocation = parsabilityMethod switch
        {
            ParsabilityMethod.IParsable => (string inputArgument, string outputArgument) => $$"""{{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.TryParse({{inputArgument}}, CultureInfo.InvariantCulture, out var {{outputArgument}})""",
            ParsabilityMethod.TryParseWithFormatProvider => (string inputArgument, string outputArgument) => $$"""{{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.TryParse({{inputArgument}}, CultureInfo.InvariantCulture, out var {{outputArgument}})""",
            ParsabilityMethod.TryParse => (string inputArgument, string outputArgument) => $$"""{{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.TryParse({{inputArgument}}, out var {{outputArgument}})""",
            ParsabilityMethod.Enum => (string inputArgument, string outputArgument) => $$"""Enum.TryParse<{{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>({{inputArgument}}, out var {{outputArgument}})""",
            _ => null // ... everything that is parsable is covered above except strings ...
        };

        // ... so for strings (null) we bail.
        if (preferredTryParseInvocation == null)
        {
            parsingBlockEmitter = null;
            return false;
        }

        // Wrap the TryParse method call in an if-block and if it doesn't work set param check failure.
        parsingBlockEmitter = (inputArgument, outputArgument) => $$"""
                        if (!{{preferredTryParseInvocation(inputArgument, outputArgument)}})
                        {
                            wasParamCheckFailure = true;
                        }
""";
        return true;
    }

    public ITypeSymbol Type { get; }
    public EndpointParameterSource Source { get; }

    // Only used for SpecialType parameters that need
    // to be resolved by a specific WellKnownType
    internal string? AssigningCode { get; set; }
    public string Name { get; }
    public bool IsOptional { get; }
    [MemberNotNull("ParsingBlockEmitter")]
    public bool IsParsable { get; }
    public Func<string, string, string> ParsingBlockEmitter { get; }

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
        out bool isOptional)
    {
        isOptional = false;
        if (!parameter.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromBodyMetadata), out var fromBodyAttribute))
        {
            return false;
        }
        isOptional |= fromBodyAttribute.TryGetNamedArgumentValue<int>("EmptyBodyBehavior", out var emptyBodyBehaviorValue) && emptyBodyBehaviorValue == 1;
        isOptional |= fromBodyAttribute.TryGetNamedArgumentValue<bool>("AllowEmpty", out var allowEmptyValue) && allowEmptyValue;
        isOptional |= (parameter.NullableAnnotation == NullableAnnotation.Annotated || parameter.HasExplicitDefaultValue);
        return true;
    }

    private static string GetParameterName(AttributeData attribute, string parameterName) =>
        attribute.TryGetNamedArgumentValue<string>("Name", out var fromSourceName)
            ? (fromSourceName ?? parameterName)
            : parameterName;

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
