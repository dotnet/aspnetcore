// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
    public EndpointParameter(Endpoint endpoint, IParameterSymbol parameter, WellKnownTypes wellKnownTypes): this(endpoint, parameter.Type, parameter.Name, wellKnownTypes)
    {
        Ordinal = parameter.Ordinal;
        IsOptional = parameter.IsOptional();
        HasDefaultValue = parameter.HasExplicitDefaultValue;
        DefaultValue = parameter.GetDefaultValueString();
        ProcessEndpointParameterSource(endpoint, parameter, parameter.GetAttributes(), wellKnownTypes);
    }

    private EndpointParameter(Endpoint endpoint, IPropertySymbol property, IParameterSymbol? parameter, WellKnownTypes wellKnownTypes) : this(endpoint, property.Type, property.Name, wellKnownTypes)
    {
        Ordinal = parameter?.Ordinal ?? 0;
        IsProperty = true;
        IsOptional = property.IsOptional() || parameter?.IsOptional() == true;
        HasDefaultValue = parameter?.HasExplicitDefaultValue ?? false;
        DefaultValue = parameter?.GetDefaultValueString() ?? "null";
        // Coalesce attributes on the property and attributes on the matching parameter
        var attributeBuilder = ImmutableArray.CreateBuilder<AttributeData>();
        attributeBuilder.AddRange(property.GetAttributes());
        if (parameter is not null)
        {
            attributeBuilder.AddRange(parameter.GetAttributes());
        }

        var propertyInfo = $"typeof({property.ContainingType.ToDisplayString()})!.GetProperty({SymbolDisplay.FormatLiteral(property.Name, true)})!";
        PropertyAsParameterInfoConstruction = parameter is not null
            ? $"new PropertyAsParameterInfo({(IsOptional ? "true" : "false")}, {propertyInfo}, {parameter.GetParameterInfoFromConstructorCode()})"
            : $"new PropertyAsParameterInfo({(IsOptional ? "true" : "false")}, {propertyInfo})";
        endpoint.EmitterContext.RequiresPropertyAsParameterInfo = IsProperty;
        ProcessEndpointParameterSource(endpoint, property, attributeBuilder.ToImmutable(), wellKnownTypes);
    }

    private EndpointParameter(Endpoint endpoint, ITypeSymbol typeSymbol, string parameterName, WellKnownTypes wellKnownTypes)
    {
        Type = typeSymbol;
        SymbolName = parameterName;
        LookupName = parameterName;
        Source = EndpointParameterSource.Unknown;
        IsArray = TryGetArrayElementType(typeSymbol, out var elementType);
        ElementType = elementType;
        IsEndpointMetadataProvider = ImplementsIEndpointMetadataProvider(typeSymbol, wellKnownTypes);
        IsEndpointParameterMetadataProvider = ImplementsIEndpointParameterMetadataProvider(typeSymbol, wellKnownTypes);
        endpoint.EmitterContext.HasEndpointParameterMetadataProvider |= IsEndpointParameterMetadataProvider;
        endpoint.EmitterContext.HasEndpointMetadataProvider |= IsEndpointMetadataProvider;
    }

    private void ProcessEndpointParameterSource(Endpoint endpoint, ISymbol symbol, ImmutableArray<AttributeData> attributes, WellKnownTypes wellKnownTypes)
    {
        if (attributes.TryGetAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromRouteMetadata), out var fromRouteAttribute))
        {
            Source = EndpointParameterSource.Route;
            LookupName = GetEscapedParameterName(fromRouteAttribute, symbol.Name);
            IsParsable = TryGetParsability(Type, wellKnownTypes, out var parsingBlockEmitter);
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else if (attributes.TryGetAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromQueryMetadata), out var fromQueryAttribute))
        {
            Source = EndpointParameterSource.Query;
            LookupName = GetEscapedParameterName(fromQueryAttribute, symbol.Name);
            IsParsable = TryGetParsability(Type, wellKnownTypes, out var parsingBlockEmitter);
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else if (attributes.TryGetAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromHeaderMetadata), out var fromHeaderAttribute))
        {
            Source = EndpointParameterSource.Header;
            LookupName = GetEscapedParameterName(fromHeaderAttribute, symbol.Name);
            IsParsable = TryGetParsability(Type, wellKnownTypes, out var parsingBlockEmitter);
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else if (attributes.TryGetAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromFormMetadata), out var fromFormAttribute))
        {
            endpoint.IsAwaitable = true;
            Source = EndpointParameterSource.FormBody;
            LookupName = GetEscapedParameterName(fromFormAttribute, symbol.Name);
            if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormFileCollection)))
            {
                IsFormFile = true;
                AssigningCode = "httpContext.Request.Form.Files";
            }
            else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormFile)))
            {
                IsFormFile = true;
                AssigningCode = $"httpContext.Request.Form.Files[{SymbolDisplay.FormatLiteral(LookupName, true)}]";
            }
            else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormCollection)))
            {
                AssigningCode = "httpContext.Request.Form";
            }
            // Minimal APIs shares the same implementation that Blazor uses for complex form binding at runtime.
            // This implementation doesn't support source generation so RDG only supports simple binding for form-based
            // arguments. If we encounter a complex object being bound from a form, emit a diagnostic and fallback to
            // dynamic code-gen.
            else if (!UsesSimpleBinding(wellKnownTypes))
            {
                var location = endpoint.Operation.Syntax.GetLocation();
                endpoint.Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.UnableToResolveParameterDescriptor, location, symbol.Name));
            }
            else
            {
                AssigningCode = !IsArray
                    ? $"(string?)httpContext.Request.Form[{SymbolDisplay.FormatLiteral(LookupName, true)}]"
                    : $"httpContext.Request.Form[{SymbolDisplay.FormatLiteral(LookupName, true)}].ToArray()";
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
                endpoint.IsAwaitable = true;
                Source = EndpointParameterSource.JsonBody;
            }
            IsOptional = isOptional;
        }
        else if (attributes.HasAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata)))
        {
            Source = EndpointParameterSource.Service;
            if (attributes.TryGetAttribute(wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_DependencyInjection_FromKeyedServicesAttribute), out var keyedServicesAttribute))
            {
                var location = endpoint.Operation.Syntax.GetLocation();
                endpoint.Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.KeyedAndNotKeyedServiceAttributesNotSupported, location));
            }
        }
        else if (attributes.TryGetAttribute(wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_DependencyInjection_FromKeyedServicesAttribute), out var keyedServicesAttribute))
        {
            Source = EndpointParameterSource.KeyedService;
            var constructorArgument = keyedServicesAttribute.ConstructorArguments.FirstOrDefault();
            KeyedServiceKey = SymbolDisplay.FormatPrimitive(constructorArgument.Value!, true, true);
        }
        else if (attributes.HasAttribute(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_AsParametersAttribute)))
        {
            Source = EndpointParameterSource.AsParameters;
            var location = endpoint.Operation.Syntax.GetLocation();
            if (IsOptional)
            {
                endpoint.Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.InvalidAsParametersNullable, location, Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            }
            if (symbol is IPropertySymbol ||
                Type is not INamedTypeSymbol namedTypeSymbol ||
                !TryGetAsParametersConstructor(endpoint, namedTypeSymbol, out var isDefaultConstructor, out var matchedProperties))
            {
                if (symbol is IPropertySymbol)
                {
                    endpoint.Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.InvalidAsParametersNested, location));
                }
                return;
            }
            EndpointParameters = matchedProperties.Select(matchedParameter => new EndpointParameter(endpoint, matchedParameter.Property, matchedParameter.Parameter, wellKnownTypes));
            if (isDefaultConstructor == true)
            {
                var parameterList = string.Join(", ", EndpointParameters.Select(p => $"{p.SymbolName} = {p.EmitHandlerArgument()}"));
                AssigningCode = $"new {namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)} {{ {parameterList} }}";
            }
            else
            {
                var parameterList = string.Join(", ", EndpointParameters.Select(p => p.EmitHandlerArgument()));
                AssigningCode = $"new {namedTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}({parameterList})";
            }
        }
        else if (TryGetSpecialTypeAssigningCode(Type, wellKnownTypes, out var specialTypeAssigningCode))
        {
            Source = EndpointParameterSource.SpecialType;
            AssigningCode = specialTypeAssigningCode;
        }
        else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormFileCollection)))
        {
            endpoint.IsAwaitable = true;
            Source = EndpointParameterSource.FormBody;
            IsFormFile = true;
            AssigningCode = "httpContext.Request.Form.Files";
        }
        else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormFile)))
        {
            endpoint.IsAwaitable = true;
            Source = EndpointParameterSource.FormBody;
            IsFormFile = true;
            AssigningCode = $"httpContext.Request.Form.Files[{SymbolDisplay.FormatLiteral(LookupName, true)}]";
        }
        else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_IFormCollection)))
        {
            endpoint.IsAwaitable = true;
            Source = EndpointParameterSource.FormBody;
            LookupName = symbol.Name;
            AssigningCode = "httpContext.Request.Form";
        }
        else if (HasBindAsync(Type, wellKnownTypes, out var bindMethod, out var bindMethodSymbol))
        {
            endpoint.IsAwaitable = true;
            endpoint.EmitterContext.RequiresPropertyAsParameterInfo = IsProperty && bindMethod is BindabilityMethod.BindAsyncWithParameter or BindabilityMethod.IBindableFromHttpContext;
            Source = EndpointParameterSource.BindAsync;
            BindMethod = bindMethod;
            BindableMethodSymbol = bindMethodSymbol;
        }
        else if (Type.SpecialType == SpecialType.System_String)
        {
            Source = EndpointParameterSource.RouteOrQuery;
        }
        else if (IsArray && ElementType.SpecialType == SpecialType.System_String)
        {
            endpoint.IsAwaitable = true;
            Source = EndpointParameterSource.JsonBodyOrQuery;
        }
        else if (SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_Primitives_StringValues)))
        {
            Source = EndpointParameterSource.Query;
            IsStringValues = true;
        }
        else if (TryGetParsability(Type, wellKnownTypes, out var parsingBlockEmitter))
        {
            Source = EndpointParameterSource.RouteOrQuery;
            IsParsable = true;
            ParsingBlockEmitter = parsingBlockEmitter;
        }
        else
        {
            endpoint.IsAwaitable = true;
            Source = EndpointParameterSource.JsonBodyOrService;
        }
        endpoint.EmitterContext.HasParsable |= IsParsable;
        // Set emitter context state for parameters that need to populate
        // accepts metadata here since we know that will always be required
        endpoint.EmitterContext.HasFormBody |= Source == EndpointParameterSource.FormBody;
        endpoint.EmitterContext.HasJsonBody |= Source == EndpointParameterSource.JsonBody;
        endpoint.EmitterContext.HasJsonBodyOrService |= Source == EndpointParameterSource.JsonBodyOrService;
        endpoint.EmitterContext.HasJsonBodyOrQuery |= Source == EndpointParameterSource.JsonBodyOrQuery;
    }

    private bool UsesSimpleBinding(WellKnownTypes wellKnownTypes)
        => SymbolEqualityComparer.Default.Equals(Type, wellKnownTypes.Get(WellKnownType.Microsoft_Extensions_Primitives_StringValues))
                || Type.SpecialType == SpecialType.System_String
                || (IsArray && ElementType.SpecialType == SpecialType.System_String)
                || TryGetParsability(Type, wellKnownTypes, out var _)
                || (IsArray && TryGetParsability(ElementType, wellKnownTypes, out var _));

    private static bool ImplementsIEndpointMetadataProvider(ITypeSymbol type, WellKnownTypes wellKnownTypes)
        => type.Implements(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IEndpointMetadataProvider));

    private static bool ImplementsIEndpointParameterMetadataProvider(ITypeSymbol type, WellKnownTypes wellKnownTypes)
        => type.Implements(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IEndpointParameterMetadataProvider));

    public ITypeSymbol Type { get; }
    public ITypeSymbol ElementType { get; }
    public bool IsEndpointMetadataProvider { get; }
    public bool IsEndpointParameterMetadataProvider { get; }
    public string SymbolName { get; }
    public string LookupName { get; set; }
    public int Ordinal { get; }
    public bool IsOptional { get; set; }
    public bool IsArray { get; set; }
    public string DefaultValue { get; set; } = "null";
    public bool HasDefaultValue { get; set; }
    [MemberNotNullWhen(true, nameof(PropertyAsParameterInfoConstruction))]
    public bool IsProperty { get; set; }
    public EndpointParameterSource Source { get; set; }
    public string? PropertyAsParameterInfoConstruction { get; set; }
    public IEnumerable<EndpointParameter>? EndpointParameters { get; set; }
    public bool IsFormFile { get; set; }
    public string? KeyedServiceKey { get; set; }

    // Only used for SpecialType parameters that need
    // to be resolved by a specific WellKnownType
    public string? AssigningCode { get; set; }

    [MemberNotNullWhen(true, nameof(ParsingBlockEmitter))]
    public bool IsParsable { get; set; }
    public Action<CodeWriter, string, string>? ParsingBlockEmitter { get; set; }
    public bool IsStringValues { get; set; }

    public BindabilityMethod? BindMethod { get; set; }
    public IMethodSymbol? BindableMethodSymbol { get; set; }

    private static bool HasBindAsync(ITypeSymbol typeSymbol, WellKnownTypes wellKnownTypes, [NotNullWhen(true)] out BindabilityMethod? bindMethod, [NotNullWhen(true)] out IMethodSymbol? bindMethodSymbol)
    {
        var parameterType = typeSymbol.UnwrapTypeSymbol(unwrapArray: true, unwrapNullable: true);
        return ParsabilityHelper.GetBindability(parameterType, wellKnownTypes, out bindMethod, out bindMethodSymbol) == Bindability.Bindable;
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
                writer.WriteLine($"""{typeSymbol.ToDisplayString(EmitterConstants.DisplayFormat)} {outputArgument} = default;""");
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
        if (!attributes.TryGetAttributeImplementingInterface(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromBodyMetadata), out var fromBodyAttribute))
        {
            return false;
        }
        isOptional |= fromBodyAttribute.TryGetNamedArgumentValue<int>("EmptyBodyBehavior", out var emptyBodyBehaviorValue) && emptyBodyBehaviorValue == 1;
        isOptional |= fromBodyAttribute.TryGetNamedArgumentValue<bool>("AllowEmpty", out var allowEmptyValue) && allowEmptyValue;
        if (typeSymbol is IParameterSymbol parameter)
        {
            isOptional |= parameter.IsOptional();
        }
        else if (typeSymbol is IPropertySymbol property)
        {
            isOptional |= property.IsOptional();
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

    private static bool TryGetAsParametersConstructor(Endpoint endpoint, INamedTypeSymbol type, out bool? isDefaultConstructor, [NotNullWhen(true)] out IEnumerable<ConstructorParameter>? matchedProperties)
    {
        isDefaultConstructor = null;
        matchedProperties = null;
        var parameterTypeString = type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        var location = endpoint.Operation.Syntax.GetLocation();
        if (type.IsAbstract)
        {
            endpoint.Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.InvalidAsParametersAbstractType, location, parameterTypeString));
            return false;
        }

        var constructors = type.Constructors.Where(constructor => constructor.DeclaredAccessibility == Accessibility.Public && !constructor.IsStatic);
        var numOfConstructors = constructors.Count();
        // When leveraging parameterless constructors, we want to ensure we only emit for writable
        // properties. We do not have this constraint if we are leveraging a parameterized constructor.
        var properties = type.GetMembers().OfType<IPropertySymbol>().Where(property => property.DeclaredAccessibility == Accessibility.Public);
        var writableProperties = properties.Where(property => !property.IsReadOnly);

        if (numOfConstructors == 1)
        {
            var targetConstructor = constructors.Single();
            var lookupTable = new Dictionary<ParameterLookupKey, IPropertySymbol>();
            foreach (var property in properties)
            {
                lookupTable.Add(new ParameterLookupKey(property.Name, property.Type), property);
            }

            var parameters = targetConstructor.GetParameters();
            var propertiesWithParameterInfo = new List<ConstructorParameter>();

            if (parameters.Length == 0)
            {
                isDefaultConstructor = true;
                matchedProperties = writableProperties.Select(property => new ConstructorParameter(property, null));
                return true;
            }

            foreach (var parameter in parameters)
            {
                var key = new ParameterLookupKey(parameter.Name!, parameter.Type);
                if (lookupTable.TryGetValue(key, out var property))
                {
                    propertiesWithParameterInfo.Add(new ConstructorParameter(property, parameter));
                }
                else
                {
                    endpoint.Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.InvalidAsParametersSignature, location, parameterTypeString));
                    return false;
                }
            }

            isDefaultConstructor = false;
            matchedProperties = propertiesWithParameterInfo;
            return true;
        }

        if (type.IsValueType)
        {
            isDefaultConstructor = true;
            matchedProperties = writableProperties.Select(property => new ConstructorParameter(property, null));
            return true;
        }

        if (numOfConstructors > 1)
        {
            endpoint.Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.InvalidAsParametersSingleConstructorOnly, location, parameterTypeString));
            return false;
        }

        endpoint.Diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.InvalidAsParametersNoConstructorFound, location, parameterTypeString));
        return false;
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
        SymbolEqualityComparer.IncludeNullability.Equals(other.Type, Type) &&
        other.KeyedServiceKey == KeyedServiceKey;

    public bool SignatureEquals(object obj) =>
        obj is EndpointParameter other &&
        SymbolEqualityComparer.IncludeNullability.Equals(other.Type, Type) &&
        // The name of the parameter matters when we are querying for a specific parameter using
        // an indexer, like `context.Request.RouteValues["id"]` or `context.Request.Query["id"]`
        // and when generating log messages for required bodies or services.
        other.SymbolName == SymbolName &&
        other.KeyedServiceKey == KeyedServiceKey;

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(SymbolName);
        hashCode.Add(Type, SymbolEqualityComparer.IncludeNullability);
        return hashCode.ToHashCode();
    }
}
