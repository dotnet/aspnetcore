// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using WellKnownType = Microsoft.AspNetCore.App.Analyzers.Infrastructure.WellKnownTypeData.WellKnownType;

namespace Microsoft.AspNetCore.Http.Generators.StaticRouteHandlerModel;

internal class EndpointParameter
{
    public EndpointParameter(Endpoint endpoint, IParameterSymbol parameter, WellKnownTypes wellKnownTypes)
    {
        Type = parameter.Type;
        Name = parameter.Name;
        LookupName = parameter.Name; // Default lookup name is same as parameter name (which is a valid C# identifier).
        Ordinal = parameter.Ordinal;
        Source = EndpointParameterSource.Unknown;
        IsOptional = parameter.IsOptional();
        IsArray = TryGetArrayElementType(parameter, out var elementType);
        ElementType = elementType;

        if (parameter.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromRouteMetadata), out var fromRouteAttribute))
        {
            Source = EndpointParameterSource.Route;
            Name = parameter.Name;
            LookupName = GetEscapedParameterName(fromRouteAttribute, parameter.Name);
            IsParsable = TryGetParsability(parameter, wellKnownTypes, out var parsingBlockEmitter);
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else if (parameter.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromQueryMetadata), out var fromQueryAttribute))
        {
            Source = EndpointParameterSource.Query;
            Name = parameter.Name;
            LookupName = GetEscapedParameterName(fromQueryAttribute, parameter.Name);
            IsParsable = TryGetParsability(parameter, wellKnownTypes, out var parsingBlockEmitter);
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else if (parameter.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromHeaderMetadata), out var fromHeaderAttribute))
        {
            Source = EndpointParameterSource.Header;
            Name = parameter.Name;
            LookupName = GetEscapedParameterName(fromHeaderAttribute, parameter.Name);
            IsParsable = TryGetParsability(parameter, wellKnownTypes, out var parsingBlockEmitter);
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else if (parameter.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromFormMetadata), out _))
        {
            Source = EndpointParameterSource.Unknown;
        }
        else if (TryGetExplicitFromJsonBody(parameter, wellKnownTypes, out var isOptional))
        {
            if (SymbolEqualityComparer.Default.Equals(parameter.Type, wellKnownTypes.Get(WellKnownType.System_IO_Stream)))
            {
                Source = EndpointParameterSource.SpecialType;
                AssigningCode = "httpContext.Request.Body";

            }
            else if (SymbolEqualityComparer.Default.Equals(parameter.Type, wellKnownTypes.Get(WellKnownType.System_IO_Pipelines_PipeReader)))
            {
                Source = EndpointParameterSource.SpecialType;
                AssigningCode = "httpContext.Request.BodyReader";
            }
            else
            {
                Source = EndpointParameterSource.JsonBody;
            }
            IsOptional = isOptional;
        }
        else if (parameter.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata)))
        {
            Source = EndpointParameterSource.Service;
        }
        else if (parameter.HasAttribute(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_AsParametersAttribute)))
        {
            Source = EndpointParameterSource.Unknown;
        }
        else if (TryGetSpecialTypeAssigningCode(Type, wellKnownTypes, out var specialTypeAssigningCode))
        {
            Source = EndpointParameterSource.SpecialType;
            AssigningCode = specialTypeAssigningCode;
        }
        else if (SymbolEqualityComparer.Default.Equals(parameter.Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormFile)) ||
                 SymbolEqualityComparer.Default.Equals(parameter.Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormFileCollection)) ||
                 SymbolEqualityComparer.Default.Equals(parameter.Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormCollection)))
        {
            Source = EndpointParameterSource.Unknown;
        }
        else if (HasBindAsync(parameter, wellKnownTypes, out var bindMethod))
        {
            Source = EndpointParameterSource.BindAsync;
            BindMethod = bindMethod;
        }
        else if (parameter.Type.SpecialType == SpecialType.System_String)
        {
            Source = EndpointParameterSource.RouteOrQuery;
        }
        else if (ShouldDisableInferredBodyParameters(endpoint.HttpMethod) && IsArray && elementType.SpecialType == SpecialType.System_String)
        {
            Source = EndpointParameterSource.Query;
        }
        else if (ShouldDisableInferredBodyParameters(endpoint.HttpMethod) && SymbolEqualityComparer.Default.Equals(parameter.Type, wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_Primitives_StringValues)))
        {
            Source = EndpointParameterSource.Query;
            IsStringValues = true;
        }
        else if (TryGetParsability(parameter, wellKnownTypes, out var parsingBlockEmitter))
        {
            Source = EndpointParameterSource.RouteOrQuery;
            IsParsable = true;
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else
        {
            Source = EndpointParameterSource.JsonBodyOrService;
        }
    }

    private static bool ShouldDisableInferredBodyParameters(string httpMethod)
    {
        switch (httpMethod)
        {
            case "MapPut" or "MapPatch" or "MapPost":
                return false;
            default:
                return true;
        }
    }

    public ITypeSymbol Type { get; }
    public ITypeSymbol ElementType { get; }

    public string Name { get; }
    public string LookupName { get; }
    public int Ordinal { get; }
    public bool IsOptional { get; }
    public bool IsArray { get; set; }

    public EndpointParameterSource Source { get; }

    // Only used for SpecialType parameters that need
    // to be resolved by a specific WellKnownType
    public string? AssigningCode { get; }

    [MemberNotNullWhen(true, nameof(ParsingBlockEmitter))]
    public bool IsParsable { get; }
    public Action<CodeWriter, string, string>? ParsingBlockEmitter { get; }
    public bool IsStringValues { get; }

    public BindabilityMethod? BindMethod { get; }

    private static bool HasBindAsync(IParameterSymbol parameter, WellKnownTypes wellKnownTypes, [NotNullWhen(true)] out BindabilityMethod? bindMethod)
    {
        var parameterType = parameter.Type.UnwrapTypeSymbol(unwrapArray: true, unwrapNullable: true);
        return ParsabilityHelper.GetBindability(parameterType, wellKnownTypes, out bindMethod) == Bindability.Bindable;
    }

    private static bool TryGetArrayElementType(IParameterSymbol parameter, [NotNullWhen(true)]out ITypeSymbol elementType)
    {
        if (parameter.Type.TypeKind == TypeKind.Array)
        {
            elementType = parameter.Type.UnwrapTypeSymbol(unwrapArray: true, unwrapNullable: false);
            return true;
        }
        else
        {
            elementType = null!;
            return false;
        }
    }

    private bool TryGetParsability(IParameterSymbol parameter, WellKnownTypes wellKnownTypes, [NotNullWhen(true)] out Action<CodeWriter, string, string>? parsingBlockEmitter)
    {
        var parameterType = parameter.Type.UnwrapTypeSymbol(unwrapArray: true, unwrapNullable: true);

        // ParsabilityHelper returns a single enumeration with a Parsable/NonParsable enumeration result. We use this already
        // in the analyzers to determine whether we need to warn on whether a type needs to implement TryParse/IParsable<T>. To
        // support usage in the code generator an optional out parameter has been added to hint at what variant of the various
        // TryParse methods should be used (this implies that the preferences are baked into ParsabilityHelper). If we aren't
        // parsable at all we bail.
        if (ParsabilityHelper.GetParsability(parameterType, wellKnownTypes, out var parsabilityMethod) != Parsability.Parsable)
        {
            parsingBlockEmitter = null;
            return false;
        }

        // If we are parsable we need to emit code based on the enumeration ParsabilityMethod which has a bunch of members
        // which spell out the preferred TryParse usage. This switch statement makes slight variations to them based on
        // which method was encountered.
        Func<string, string, string>? preferredTryParseInvocation = parsabilityMethod switch
        {
            ParsabilityMethod.IParsable => (string inputArgument, string outputArgument) => $$"""GeneratedRouteBuilderExtensionsCore.TryParseExplicit<{{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>({{inputArgument}}!, CultureInfo.InvariantCulture, out var {{outputArgument}})""",
            ParsabilityMethod.TryParseWithFormatProvider => (string inputArgument, string outputArgument) => $$"""{{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.TryParse({{inputArgument}}!, CultureInfo.InvariantCulture, out var {{outputArgument}})""",
            ParsabilityMethod.TryParse => (string inputArgument, string outputArgument) => $$"""{{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.TryParse({{inputArgument}}!, out var {{outputArgument}})""",
            ParsabilityMethod.Enum => (string inputArgument, string outputArgument) => $$"""Enum.TryParse<{{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}>({{inputArgument}}!, out var {{outputArgument}})""",
            ParsabilityMethod.Uri => (string inputArgument, string outputArgument) => $$"""Uri.TryCreate({{inputArgument}}!, UriKind.RelativeOrAbsolute, out var {{outputArgument}})""",
            ParsabilityMethod.String => null, // string parameters don't require parsing
            _ => throw new Exception("Unreachable!"),
        };

        // Special case handling for specific types
        if (parameterType.SpecialType == SpecialType.System_Char)
        {
            preferredTryParseInvocation = (string inputArgument, string outputArgument) => $$"""{{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.TryParse({{inputArgument}}!, out var {{outputArgument}})""";
        }
        else if (parameterType.SpecialType == SpecialType.System_DateTime)
        {
            preferredTryParseInvocation = (string inputArgument, string outputArgument) => $$"""{{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.TryParse({{inputArgument}}!, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces, out var {{outputArgument}})""";
        }
        else if (SymbolEqualityComparer.Default.Equals(parameterType, wellKnownTypes.Get(WellKnownType.System_DateTimeOffset)))
        {
            preferredTryParseInvocation = (string inputArgument, string outputArgument) => $$"""{{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.TryParse({{inputArgument}}!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces, out var {{outputArgument}})""";
        }
        else if (SymbolEqualityComparer.Default.Equals(parameterType, wellKnownTypes.Get(WellKnownType.System_DateOnly)))
        {
            preferredTryParseInvocation = (string inputArgument, string outputArgument) => $$"""{{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}}.TryParse({{inputArgument}}!, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var {{outputArgument}})""";
        }

        // ... so for strings (null) we bail.
        if (preferredTryParseInvocation == null)
        {
            parsingBlockEmitter = null;
            return false;
        }

        if (IsOptional)
        {
            parsingBlockEmitter = (writer, inputArgument, outputArgument) =>
            {
                writer.WriteLine($"""{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {outputArgument} = default;""");
                writer.WriteLine($$"""if ({{preferredTryParseInvocation(inputArgument, $"{inputArgument}_parsed_non_nullable")}})""");
                writer.StartBlock();
                writer.WriteLine($$"""{{outputArgument}} = {{$"{inputArgument}_parsed_non_nullable"}};""");
                writer.EndBlock();
                writer.WriteLine($$"""else if (string.IsNullOrEmpty({{inputArgument}}))""");
                writer.StartBlock();
                writer.WriteLine($$"""{{outputArgument}} = null;""");
                writer.EndBlock();
                writer.WriteLine("else");
                writer.StartBlock();
                writer.WriteLine("wasParamCheckFailure = true;");
                writer.EndBlock();
            };
        }
        else
        {
            parsingBlockEmitter = (writer, inputArgument, outputArgument) =>
            {
                if (IsArray && ElementType.NullableAnnotation == NullableAnnotation.Annotated)
                {
                    writer.WriteLine($$"""if (!{{preferredTryParseInvocation(inputArgument, outputArgument)}})""");
                    writer.StartBlock();
                    writer.WriteLine($$"""if (!string.IsNullOrEmpty({{inputArgument}}))""");
                    writer.StartBlock();
                    writer.WriteLine("wasParamCheckFailure = true;");
                    writer.EndBlock();
                    writer.EndBlock();
                }
                else
                {
                    writer.WriteLine($$"""if (!{{preferredTryParseInvocation(inputArgument, outputArgument)}})""");
                    writer.StartBlock();
                    writer.WriteLine("wasParamCheckFailure = true;");
                    writer.EndBlock();
                }
            };
        }

        // Wrap the TryParse method call in an if-block and if it doesn't work set param check failure.
        return true;
    }

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

    private static string GetEscapedParameterName(AttributeData attribute, string parameterName)
    {
        if (attribute.TryGetNamedArgumentValue<string>("Name", out var fromSourceName) && fromSourceName is not null)
        {
            // TODO: This is a quick hack to stop someone trying to inject code into
            //       a lookup key as part of a dictionary. We should decide whether
            //       we want to:
            //
            //       a) Accept any input but escape it.
            //       b) Narrowly scope what we accept so it is constrained to what are acceptable in HTTP paths, querystrings, and headers.
            return ConvertEndOfLineAndQuotationCharactersToEscapeForm(fromSourceName);
        }
        else
        {
            return parameterName;
        }
    }

    // Lifted from:
    // https://github.com/dotnet/runtime/blob/dc5a6c8be1644915c14c4a464447b0d54e223a46/src/libraries/Microsoft.Extensions.Logging.Abstractions/gen/LoggerMessageGenerator.Emitter.cs#L562
    private static string ConvertEndOfLineAndQuotationCharactersToEscapeForm(string s)
    {
        int index = 0;
        while (index < s.Length)
        {
            if (s[index] is '\n' or '\r' or '"' or '\\')
            {
                break;
            }
            index++;
        }

        if (index >= s.Length)
        {
            return s;
        }

        StringBuilder sb = new StringBuilder(s.Length);
        sb.Append(s, 0, index);

        while (index < s.Length)
        {
            switch (s[index])
            {
                case '\n':
                    sb.Append('\\');
                    sb.Append('n');
                    break;

                case '\r':
                    sb.Append('\\');
                    sb.Append('r');
                    break;

                case '"':
                    sb.Append('\\');
                    sb.Append('"');
                    break;

                case '\\':
                    sb.Append("\\");
                    sb.Append("\\");
                    break;

                default:
                    sb.Append(s[index]);
                    break;
            }

            index++;
        }

        return sb.ToString();
    }

    public override bool Equals(object obj) =>
        obj is EndpointParameter other &&
        other.Source == Source &&
        other.Name == Name &&
        other.Ordinal == Ordinal &&
        other.IsOptional == IsOptional &&
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
