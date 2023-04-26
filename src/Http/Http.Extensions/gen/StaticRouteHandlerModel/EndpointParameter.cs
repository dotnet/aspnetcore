// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using WellKnownType = Microsoft.AspNetCore.App.Analyzers.Infrastructure.WellKnownTypeData.WellKnownType;

namespace Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;

internal class EndpointParameter
{
    public EndpointParameter(Endpoint endpoint, IParameterSymbol parameter, WellKnownTypes wellKnownTypes)
    {
        Type = parameter.Type;
        SymbolName = parameter.Name;
        LookupName = parameter.Name; // Default lookup name is same as property name (which is a valid C# identifier).
        Ordinal = parameter.Ordinal;
        Source = EndpointParameterSource.Unknown;
        IsOptional = parameter.IsOptional();
        DefaultValue = parameter.GetDefaultValueString();
        IsArray = TryGetArrayElementType(Type, out var elementType);
        ElementType = elementType;
        IsEndpointMetadataProvider = ImplementsIEndpointMetadataProvider(parameter, wellKnownTypes);
        IsEndpointParameterMetadataProvider = ImplementsIEndpointParameterMetadataProvider(parameter, wellKnownTypes);
        ProcessEndpointParameterSource(endpoint, parameter, parameter.GetAttributes(), wellKnownTypes);
    }

    public EndpointParameter(Endpoint endpoint, IPropertySymbol property, IParameterSymbol? parameter, WellKnownTypes wellKnownTypes)
    {
        Type = property.Type;
        SymbolName = property.Name;
        LookupName = property.Name;
        Ordinal = 0;
        Source = EndpointParameterSource.Unknown;
        IsOptional = property.IsOptional() || parameter?.IsOptional() == true;
        DefaultValue = parameter?.GetDefaultValueString() ?? "null";
        IsArray = TryGetArrayElementType(Type, out var elementType);
        ElementType = elementType;
        var attributeBuilder = ImmutableArray.CreateBuilder<AttributeData>();
        attributeBuilder.AddRange(property.GetAttributes());
        if (parameter is not null)
        {
            attributeBuilder.AddRange(parameter.GetAttributes());
        }
        ProcessEndpointParameterSource(endpoint, property, attributeBuilder.ToImmutable(), wellKnownTypes);
    }

    private void ProcessEndpointParameterSource(Endpoint endpoint, ISymbol symbol, ImmutableArray<AttributeData> attributes, WellKnownTypes wellKnownTypes)
    {
        if (attributes.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromRouteMetadata), out var fromRouteAttribute))
        {
            Source = EndpointParameterSource.Route;
            LookupName = GetEscapedParameterName(fromRouteAttribute, symbol.Name);
            IsParsable = TryGetParsability(Type, wellKnownTypes, out var parsingBlockEmitter);
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else if (attributes.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromQueryMetadata), out var fromQueryAttribute))
        {
            Source = EndpointParameterSource.Query;
            LookupName = GetEscapedParameterName(fromQueryAttribute, symbol.Name);
            IsParsable = TryGetParsability(Type, wellKnownTypes, out var parsingBlockEmitter);
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else if (attributes.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromHeaderMetadata), out var fromHeaderAttribute))
        {
            Source = EndpointParameterSource.Header;
            LookupName = GetEscapedParameterName(fromHeaderAttribute, symbol.Name);
            IsParsable = TryGetParsability(Type, wellKnownTypes, out var parsingBlockEmitter);
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else if (attributes.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromFormMetadata), out var fromFormAttribute))
        {
            Source = EndpointParameterSource.FormBody;
            LookupName = GetEscapedParameterName(fromFormAttribute, symbol.Name);
            if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormFileCollection)))
            {
                AssigningCode = "httpContext.Request.Form.Files";
            }
            else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormFile)))
            {
                AssigningCode = $"httpContext.Request.Form.Files[{SymbolDisplay.FormatLiteral(LookupName, true)}]";
            }
            else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormCollection)))
            {
                AssigningCode = "httpContext.Request.Form";
            }
            else
            {
                AssigningCode = $"(string?)httpContext.Request.Form[{SymbolDisplay.FormatLiteral(LookupName, true)}]";
                IsParsable = TryGetParsability(Type, wellKnownTypes, out var parsingBlockEmitter);
                ParsingBlockEmitter = parsingBlockEmitter;
            }
        }
        else if (TryGetExplicitFromJsonBody(symbol, attributes, wellKnownTypes, out var isOptional))
        {
            if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.System_IO_Stream)))
            {
                Source = EndpointParameterSource.SpecialType;
                AssigningCode = "httpContext.Request.Body";
            }
            else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.System_IO_Pipelines_PipeReader)))
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
        else if (attributes.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata)))
        {
            Source = EndpointParameterSource.Service;
        }
        else if (attributes.HasAttribute(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_AsParametersAttribute)))
        {
            Source = EndpointParameterSource.Unknown;
            if (symbol is IPropertySymbol)
            {
                throw new InvalidOperationException("Can't have AsParameters on a property.");
            }
            if (Type is INamedTypeSymbol namedTypeSymbol && TryGetAsParametersConstructor(namedTypeSymbol, out var isParameterlessConstructor, out var matchedParameters))
            {
                Source = EndpointParameterSource.AsParameters;
                EndpointParameters = matchedParameters.Select(matchedParameter => (matchedParameter, new EndpointParameter(endpoint, matchedParameter.Property, matchedParameter.Parameter, wellKnownTypes)));
                if (isParameterlessConstructor == true)
                {
                    var parameterTypeList = string.Join(", ", EndpointParameters.Select(p => $"{p.Item1.Property.Name} = {p.Item2.EmitHandlerArgument()}"));
                    AssigningCode = $"new {namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {{ {parameterTypeList} }}";
                }
                else
                {
                    var parameterTypeList = string.Join(", ", EndpointParameters.Select(p => p.Item2.EmitHandlerArgument()));
                    AssigningCode = $"new {namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({parameterTypeList})";
                }
            }
        }
        else if (TryGetSpecialTypeAssigningCode(Type, wellKnownTypes, out var specialTypeAssigningCode))
        {
            Source = EndpointParameterSource.SpecialType;
            AssigningCode = specialTypeAssigningCode;
        }
        else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormFileCollection)))
        {
            Source = EndpointParameterSource.FormBody;
            LookupName = symbol.Name;
            AssigningCode = "httpContext.Request.Form.Files";
        }
        else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormFile)))
        {
            Source = EndpointParameterSource.FormBody;
            LookupName = symbol.Name;
            AssigningCode = $"httpContext.Request.Form.Files[{SymbolDisplay.FormatLiteral(LookupName, true)}]";
        }
        else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormCollection)))
        {
            Source = EndpointParameterSource.FormBody;
            LookupName = symbol.Name;
            AssigningCode = "httpContext.Request.Form";
        }
        else if (HasBindAsync(Type, wellKnownTypes, out var bindMethod))
        {
            Source = EndpointParameterSource.BindAsync;
            BindMethod = bindMethod;
        }
        else if (Type.SpecialType == SpecialType.System_String)
        {
            Source = EndpointParameterSource.RouteOrQuery;
        }
        else if (ShouldDisableInferredBodyParameters(endpoint.HttpMethod) && IsArray && ElementType.SpecialType == SpecialType.System_String)
        {
            Source = EndpointParameterSource.Query;
        }
        else if (ShouldDisableInferredBodyParameters(endpoint.HttpMethod) && SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_Primitives_StringValues)))
        {
            Source = EndpointParameterSource.Query;
            IsStringValues = true;
        }
        else if (TryGetParsability(Type, wellKnownTypes, out var parsingBlockEmitter))
        {
            Source = EndpointParameterSource.RouteOrQuery;
            IsParsable = true;
            endpoint.EmitterContext.HasParsable = true;
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else
        {
            Source = EndpointParameterSource.JsonBodyOrService;
        }
    }

    private static bool ImplementsIEndpointMetadataProvider(IParameterSymbol parameter, WellKnownTypes wellKnownTypes)
        => parameter.Type.Implements(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IEndpointMetadataProvider));

    private static bool ImplementsIEndpointParameterMetadataProvider(IParameterSymbol parameter, WellKnownTypes wellKnownTypes)
        => parameter.Type.Implements(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IEndpointParameterMetadataProvider));

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
    public bool IsEndpointMetadataProvider { get; }
    public bool IsEndpointParameterMetadataProvider { get; }
    public string SymbolName { get; }
    public string LookupName { get; set;  }
    public int Ordinal { get; }
    public bool IsOptional { get; set; }
    public bool IsArray { get; set; }
    public string DefaultValue { get; set; }

    public EndpointParameterSource Source { get; set; }

    public IEnumerable<(ConstructorParameter, EndpointParameter)>? EndpointParameters { get; set; }

    // Only used for SpecialType parameters that need
    // to be resolved by a specific WellKnownType
    public string? AssigningCode { get; set; }

    [MemberNotNullWhen(true, nameof(ParsingBlockEmitter))]
    public bool IsParsable { get; set; }
    public Action<CodeWriter, string, string>? ParsingBlockEmitter { get; set; }
    public bool IsStringValues { get; set; }

    public BindabilityMethod? BindMethod { get; set; }

    private static bool HasBindAsync(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes, [NotNullWhen(true)] out BindabilityMethod? bindMethod)
    {
        var parameterType = typeSymbol.UnwrapTypeSymbol(unwrapArray: true, unwrapNullable: true);
        return ParsabilityHelper.GetBindability(parameterType, wellKnownTypes, out bindMethod) == Bindability.Bindable;
    }

    private static bool TryGetArrayElementType(ITypeSymbol type, [NotNullWhen(true)]out ITypeSymbol elementType)
    {
        if (type.TypeKind == TypeKind.Array)
        {
            elementType = type.UnwrapTypeSymbol(unwrapArray: true, unwrapNullable: false);
            return true;
        }
        else
        {
            elementType = null!;
            return false;
        }
    }

    private bool TryGetParsability(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes, [NotNullWhen(true)] out Action<CodeWriter, string, string>? parsingBlockEmitter)
    {
        var parameterType = typeSymbol.UnwrapTypeSymbol(unwrapArray: true, unwrapNullable: true);

        // ParsabilityHelper returns a single enumeration with a Parsable/NonParsable enumeration result. We use this already
        // in the analyzers to determine whether we need to warn on whether a type needs to implement TryParse/IParsable<T>. To
        // support usage in the code generator an optional out property has been added to hint at what variant of the various
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
            _ => throw new NotImplementedException($"Unreachable! Unexpected {nameof(ParsabilityMethod)}: {parsabilityMethod}"),
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
                writer.WriteLine($"""{typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {outputArgument} = default;""");
                writer.WriteLine($$"""if ({{preferredTryParseInvocation(inputArgument, $"{inputArgument}_parsed_non_nullable")}})""");
                writer.StartBlock();
                writer.WriteLine($$"""{{outputArgument}} = {{$"{inputArgument}_parsed_non_nullable"}};""");
                writer.EndBlock();
                writer.WriteLine($$"""else if (string.IsNullOrEmpty({{inputArgument}}))""");
                writer.StartBlock();
                writer.WriteLine($$"""{{outputArgument}} = {{DefaultValue}};""");
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
                    writer.WriteLine($@"logOrThrowExceptionHelper.RequiredParameterNotProvided({SymbolDisplay.FormatLiteral(Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), true)}, {SymbolDisplay.FormatLiteral(SymbolName, true)}, {SymbolDisplay.FormatLiteral(this.ToMessageString(), true)});");
                    writer.EndBlock();
                    writer.EndBlock();
                }
                else
                {
                    writer.WriteLine($$"""if (!{{preferredTryParseInvocation(inputArgument, outputArgument)}})""");
                    writer.StartBlock();
                    writer.WriteLine($"if (!string.IsNullOrEmpty({inputArgument}))");
                    writer.StartBlock();
                    writer.WriteLine($@"logOrThrowExceptionHelper.ParameterBindingFailed({SymbolDisplay.FormatLiteral(Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat), true)}, {SymbolDisplay.FormatLiteral(SymbolName, true)}, {inputArgument});");
                    writer.WriteLine("wasParamCheckFailure = true;");
                    writer.EndBlock();
                    writer.EndBlock();
                }
            };
        }

        // Wrap the TryParse method call in an if-block and if it doesn't work set param check failure.
        return true;
    }

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

    private static bool TryGetExplicitFromJsonBody(ISymbol typeSymbol,
        ImmutableArray<AttributeData> attributes,
        WellKnownTypes wellKnownTypes,
        out bool isOptional)
    {
        isOptional = false;
        if (!attributes.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromBodyMetadata), out var fromBodyAttribute))
        {
            return false;
        }
        isOptional |= fromBodyAttribute.TryGetNamedArgumentValue<int>("EmptyBodyBehavior", out var emptyBodyBehaviorValue) && emptyBodyBehaviorValue == 1;
        isOptional |= fromBodyAttribute.TryGetNamedArgumentValue<bool>("AllowEmpty", out var allowEmptyValue) && allowEmptyValue;
        if (typeSymbol is IParameterSymbol parameter)
        {
            isOptional |= (parameter.NullableAnnotation == NullableAnnotation.Annotated || parameter.HasExplicitDefaultValue);
        }
        else if (typeSymbol is IPropertySymbol property)
        {
            isOptional |= property.NullableAnnotation == NullableAnnotation.Annotated;
        }
        return true;
    }

    private static string GetEscapedParameterName(AttributeData attribute, string parameterName)
    {
        if (attribute.TryGetNamedArgumentValue<string>("Name", out var fromSourceName) && fromSourceName is not null)
        {
            return ConvertEndOfLineAndQuotationCharactersToEscapeForm(fromSourceName);
        }
        else
        {
            return parameterName;
        }
    }

    private static bool TryGetAsParametersConstructor(INamedTypeSymbol type, out bool? isParameterlessConstructor, [NotNullWhen(true)] out IEnumerable<ConstructorParameter>? matchedParameters)
    {
        isParameterlessConstructor = null;
        matchedParameters = null;
        if (type.IsAbstract)
        {
            return false;
            // throw new InvalidOperationException($"The abstract type '{type.Name}' is not supported.");
        }

        var constructors = type.Constructors.Where(constructor => constructor.DeclaredAccessibility == Accessibility.Public && !constructor.IsStatic);

        if (constructors.Count() == 1)
        {
            var targetConstructor = constructors.SingleOrDefault();
            var properties = type.GetMembers().OfType<IPropertySymbol>().Where(property => property.DeclaredAccessibility == Accessibility.Public);
            var lookupTable = new Dictionary<ParameterLookupKey, IPropertySymbol>();
            foreach (var property in properties)
            {
                lookupTable.Add(new ParameterLookupKey(property.Name, property.Type), property);
            }

            // This behavior diverge from the JSON serialization
            // since we don't have an attribute, eg. JsonConstructor,
            // we need to be very restrictive about the ctor
            // and only accept if the parameterized ctor has
            // only arguments that we can match (Type and Name)
            // with a public property.

            var parameters = targetConstructor.GetParameters();
            var propertiesWithParameterInfo = new List<ConstructorParameter>();

            if (parameters.Length == 0)
            {
                isParameterlessConstructor = true;
                matchedParameters = properties.Select(property => new ConstructorParameter(property, null));
                return true;
            }

            for (var i = 0; i < parameters.Length; i++)
            {
                var key = new ParameterLookupKey(parameters[i].Name!, parameters[i].Type);
                if (lookupTable.TryGetValue(key, out var property))
                {
                    propertiesWithParameterInfo.Add(new ConstructorParameter(property, parameters[i]));
                }
                else
                {
                    return false;
                    // throw new InvalidOperationException(
                    //     $"The public parameterized constructor must contain only parameters that match the declared public properties for type '{type.Name}'.");
                }
            }

            isParameterlessConstructor = false;
            matchedParameters = propertiesWithParameterInfo;
            return true;
        }

        var parameterlessConstructor = constructors.SingleOrDefault(c => c.GetParameters().Length == 0);
        if (parameterlessConstructor is not null)
        {
            isParameterlessConstructor = true;
            matchedParameters = type.GetMembers().OfType<IPropertySymbol>().Select(property => new ConstructorParameter(property, null));
            return true;
        }

        if (type.IsValueType)
        {
            isParameterlessConstructor = true;
            matchedParameters = type.GetMembers().OfType<IPropertySymbol>().Select(property => new ConstructorParameter(property, null));
            return true;
        }

        if (parameterlessConstructor is null && constructors.Count() > 1)
        {
            return false;
            // throw new InvalidOperationException($"Only a single public parameterized constructor is allowed for type '{type.Name}'.");
        }

        return false;
        // throw new InvalidOperationException($"No public parameterless constructor found for type '{type.Name}'.");
    }

    // Lifted from:
    // https://github.com/dotnet/runtime/blob/dc5a6c8be1644915c14c4a464447b0d54e223a46/src/libraries/Microsoft.Extensions.Logging.Abstractions/gen/LoggerMessageGenerator.Emitter.cs#L562
    private static string ConvertEndOfLineAndQuotationCharactersToEscapeForm(string s)
    {
        var index = 0;
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

        var sb = new StringBuilder(s.Length);
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
                    sb.Append('\\');
                    sb.Append('\\');
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
        other.SymbolName == SymbolName &&
        other.Ordinal == Ordinal &&
        other.IsOptional == IsOptional &&
        SymbolEqualityComparer.Default.Equals(other.Type, Type);

    public bool SignatureEquals(object obj) =>
        obj is EndpointParameter other &&
        SymbolEqualityComparer.Default.Equals(other.Type, Type);

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(SymbolName);
        hashCode.Add(Type, SymbolEqualityComparer.Default);
        return hashCode.ToHashCode();
    }
}

internal sealed class ParameterLookupKey
{
    public ParameterLookupKey(string name, ITypeSymbol type)
    {
        Name = name;
        Type = type;
    }

    public string Name { get; }
    public ITypeSymbol Type { get; }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is ParameterLookupKey other)
        {
            return SymbolEqualityComparer.Default.Equals(Type, other.Type) &&
                string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}

internal record ConstructorParameter(IPropertySymbol Property, IParameterSymbol? Parameter);
