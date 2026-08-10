// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.AspNetCore.Http.RequestDelegateGenerator.StaticRouteHandlerModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Reports cases where the validation source generator silently skips validation that the developer
/// likely intended to run.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class ValidationsDiagnosticAnalyzer : DiagnosticAnalyzer
{
    private const string Usage = "Usage";

    private static string GetHelpLinkUri(string id) => $"https://learn.microsoft.com/aspnet/core/diagnostics/{id.ToLowerInvariant()}";

    internal static readonly DiagnosticDescriptor ValidatableTypeIsNotAccessible = new(
        "ASP0033",
        "[ValidatableType] is applied to an inaccessible type",
        "The type '{0}' is marked with [ValidatableType] but is not accessible from the generated validation code. The type must be public or internal and must not be a file-local type, otherwise its validation is silently skipped.",
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: null,
        helpLinkUri: GetHelpLinkUri("ASP0033"));

    internal static readonly DiagnosticDescriptor EndpointParameterTypeIsNotAccessible = new(
        "ASP0034",
        "Endpoint parameter type is inaccessible from generated code",
        "The type '{1}' of endpoint parameter '{0}' is not accessible from the generated validation code. The type must be public or internal and must not be a file-local type, otherwise its validation is silently skipped.",
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: null,
        helpLinkUri: GetHelpLinkUri("ASP0034"),
        WellKnownDiagnosticTags.CompilationEnd);

    internal static readonly DiagnosticDescriptor ValidatablePropertyIsNotAccessible = new(
        "ASP0035",
        "Validatable property or its type is not accessible",
        "The property '{0}' on type '{1}' declares validation but is not public or its type isn't accessible in generated code, so it is silently skipped by the validation source generator",
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: null,
        helpLinkUri: GetHelpLinkUri("ASP0035"));

    internal static readonly DiagnosticDescriptor ValidatablePropertyIsNotAccessibleCompilationEnd = new(
        "ASP0036",
        "Validatable property or its type is not accessible",
        "The property '{0}' on type '{1}' declares validation but is not public or its type isn't accessible in generated code, so it is silently skipped by the validation source generator",
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: null,
        helpLinkUri: GetHelpLinkUri("ASP0036"),
        WellKnownDiagnosticTags.CompilationEnd);

    internal static readonly DiagnosticDescriptor ValidatableTypeIsUsedWithoutAddValidation = new(
        "ASP0038",
        "[ValidatableType] should not be used without a call to 'AddValidation'",
        "'[ValidatableType]' has no effect if there is no 'AddValidation' call in your application entry-point",
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: null,
        helpLinkUri: GetHelpLinkUri("ASP0038"),
        WellKnownDiagnosticTags.CompilationEnd);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        ValidatableTypeIsNotAccessible,
        EndpointParameterTypeIsNotAccessible,
        ValidatablePropertyIsNotAccessible,
        ValidatablePropertyIsNotAccessibleCompilationEnd,
        ValidatableTypeIsUsedWithoutAddValidation,
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static bool IsPropertyIgnoredDueToAccessibility(IPropertySymbol property)
        => property.DeclaredAccessibility != Accessibility.Public ||
            IsInaccessibleFromGeneratedCode(property.Type.UnwrapType());

    private static bool IsInaccessibleFromGeneratedCode(ITypeSymbol type)
        => type is INamedTypeSymbol { IsFileLocal: true } ||
            type.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal);

    private static void AnalyzeType(
        Action<Diagnostic> reportDiagnostic,
        INamedTypeSymbol skipValidationAttributeSymbol,
        INamedTypeSymbol jsonIgnoreAttributeSymbol,
        ITypeSymbol currentType,
        ConcurrentDictionary<ITypeSymbol, byte> allValidatableTypes,
        WellKnownTypes wellKnownTypes,
        bool isCalledFromCompilationEnd)
    {
        if (currentType.SpecialType != SpecialType.None || currentType.IsExemptType(wellKnownTypes))
        {
            return;
        }

        if (!allValidatableTypes.TryAdd(currentType, 0))
        {
            return;
        }

        foreach (var property in currentType.GetMembers().OfType<IPropertySymbol>())
        {
            if (ValidationsGenerator.ShouldSkipProperty(property, wellKnownTypes, skipValidationAttributeSymbol, jsonIgnoreAttributeSymbol))
            {
                continue;
            }

            if (IsPropertyIgnoredDueToAccessibility(property) &&
                (ValidationsGenerator.HasValidationAttributes(property, wellKnownTypes) ||
                TypeHasValidation(property.Type, wellKnownTypes)))
            {
                reportDiagnostic(Diagnostic.Create(
                    isCalledFromCompilationEnd ? ValidatablePropertyIsNotAccessibleCompilationEnd : ValidatablePropertyIsNotAccessible,
                    property.Locations.FirstOrDefault(),
                    property.Name,
                    currentType.ToDisplayString()));
            }
        }
    }

    private static bool TypeHasValidation(ITypeSymbol type, WellKnownTypes wellKnownTypes)
    {
        if (ValidationsGenerator.HasValidationAttributes(type, wellKnownTypes) ||
            ValidationsGenerator.HasIValidatableObjectInterface(type, wellKnownTypes))
        {
            return true;
        }

        foreach (var member in type.GetMembers())
        {
            if (member is not IPropertySymbol property)
            {
                continue;
            }

            if (ValidationsGenerator.HasValidationAttributes(property, wellKnownTypes))
            {
                return true;
            }
        }

        return false;
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var compilation = context.Compilation;
        var serviceCollectionExtensionsType = compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ValidationServiceCollectionExtensions");
        var validatableTypeAttribute = compilation.GetTypeByMetadataName("Microsoft.Extensions.Validation.ValidatableTypeAttribute");
        var validationEndpointConventionBuilderExtensions = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Builder.ValidationEndpointConventionBuilderExtensions");

        var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);
        var fromServiceMetadata = wellKnownTypes.GetOptional(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata);
        var fromKeyedServiceAttribute = wellKnownTypes.GetOptional(WellKnownTypeData.WellKnownType.Microsoft_Extensions_DependencyInjection_FromKeyedServicesAttribute);
        var skipValidationAttribute = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_Extensions_Validation_SkipValidationAttribute);
        var jsonIgnoreAttributeSymbol = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.System_Text_Json_Serialization_JsonIgnoreAttribute);

        var topLevelValidatableTypes = new ConcurrentDictionary<ITypeSymbol, byte>(SymbolEqualityComparer.Default);
        var endpointParameters = new ConcurrentDictionary<IParameterSymbol, byte>(SymbolEqualityComparer.Default);

        var addValidationFound = false;
        var validatableTypeAttributeFound = false;

        if (validatableTypeAttribute is not null)
        {
            // This callback collects types that are attributed with [ValidatableType]
            context.RegisterOperationAction(context =>
            {
                var attributeOperation = (IAttributeOperation)context.Operation;
                if (context.ContainingSymbol is INamedTypeSymbol attributedType &&
                    attributeOperation.Operation is IObjectCreationOperation attributeObjectCreationOperation &&
                    validatableTypeAttribute.Equals(attributeObjectCreationOperation.Constructor?.ContainingType, SymbolEqualityComparer.Default))
                {
                    validatableTypeAttributeFound = true;

                    if (IsInaccessibleFromGeneratedCode(attributedType))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(ValidatableTypeIsNotAccessible, attributedType.Locations.FirstOrDefault(), attributedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                        return;
                    }

                    AnalyzeType(context.ReportDiagnostic, skipValidationAttribute, jsonIgnoreAttributeSymbol, attributedType, topLevelValidatableTypes, wellKnownTypes, isCalledFromCompilationEnd: false);
                }
            }, OperationKind.Attribute);
        }

        // This callback collects:
        // 1. Whether AddValidation is called.
        // 2. The types of minimal API parameters.
        context.RegisterOperationAction(context =>
        {
            var invocation = (IInvocationOperation)context.Operation;

            if (IsAddValidationInvocation(invocation, serviceCollectionExtensionsType))
            {
                addValidationFound = true;
                return;
            }

            var semanticModel = context.Operation.SemanticModel!;
            if (InvocationOperationExtensions.KnownMethods.Contains(invocation.TargetMethod.Name) &&
                invocation.TryGetRouteHandlerMethod(semanticModel, needsAccurateSignature: false, out var routeHandlerMethod))
            {
                if (IsValidationDisabledForEndpoint(invocation, validationEndpointConventionBuilderExtensions))
                {
                    return;
                }

                foreach (var parameter in routeHandlerMethod.Parameters)
                {
                    if (parameter.IsServiceParameter(fromServiceMetadata, fromKeyedServiceAttribute)
                        || parameter.IsSkippedValidationParameter(skipValidationAttribute))
                    {
                        continue;
                    }

                    if (parameter.Type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                    {
                        // Not very accurate check for services, but fine for the case of the analyzer.
                        // We don't want to report a diagnostic for a service parameter.
                        // IEnumerable<T> isn't guaranteed to be a service though.
                        // If it wasn't a service and we skipped it, we will only have a false negative.
                        continue;
                    }

                    endpointParameters.TryAdd(parameter, 0);
                }
            }
        }, OperationKind.Invocation);

        context.RegisterCompilationEndAction(context =>
        {
            if (!addValidationFound && validatableTypeAttributeFound)
            {
                context.ReportDiagnostic(Diagnostic.Create(ValidatableTypeIsUsedWithoutAddValidation, Location.None));
                return;
            }

            if (addValidationFound)
            {
                foreach (var parameter in endpointParameters.Keys)
                {
                    var type = parameter.Type.UnwrapType();
                    if (IsInaccessibleFromGeneratedCode(type))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(EndpointParameterTypeIsNotAccessible, parameter.Locations.FirstOrDefault(), parameter.Name, type.Name));
                        continue;
                    }

                    AnalyzeType(context.ReportDiagnostic, skipValidationAttribute, jsonIgnoreAttributeSymbol, type, topLevelValidatableTypes, wellKnownTypes, isCalledFromCompilationEnd: true);
                }
            }
        });
    }

    private static bool IsAddValidationInvocation(IInvocationOperation invocation, INamedTypeSymbol? extensionsType)
    {
        var method = invocation.TargetMethod;
        return method.Name == "AddValidation" &&
            method.ContainingType.Equals(extensionsType, SymbolEqualityComparer.Default);
    }

    private static bool IsValidationDisabledForEndpoint(IInvocationOperation routeHandlerInvocation, INamedTypeSymbol? validationEndpointConventionBuilderExtensions)
    {
        if (validationEndpointConventionBuilderExtensions is null)
        {
            return false;
        }

        // Walk up the fluent chain (e.g. app.MapPost(...).WithName(...).DisableValidation())
        // to determine whether DisableValidation() is applied to this endpoint.
        var current = routeHandlerInvocation.Parent;
        while (current is not null && current is not IExpressionStatementOperation)
        {
            if (IsDisableValidationInvocation(current, validationEndpointConventionBuilderExtensions))
            {
                return true;
            }

            current = current.Parent;
        }

        // Walk down the receiver chain (e.g. app.MapGroup(...).DisableValidation().MapPost(...))
        // to determine whether DisableValidation() is applied to a group the endpoint belongs to.
        var receiver = GetInvocationReceiver(routeHandlerInvocation);
        while (receiver is not null)
        {
            if (receiver is IConversionOperation conversion)
            {
                receiver = conversion.Operand;
                continue;
            }

            if (receiver is IInvocationOperation receiverInvocation)
            {
                if (IsDisableValidationInvocation(receiverInvocation, validationEndpointConventionBuilderExtensions))
                {
                    return true;
                }

                receiver = GetInvocationReceiver(receiverInvocation);
                continue;
            }

            break;
        }

        return false;
    }

    private static bool IsDisableValidationInvocation(IOperation operation, INamedTypeSymbol validationEndpointConventionBuilderExtensions)
        => operation is IInvocationOperation invocation &&
            invocation.TargetMethod.Name == "DisableValidation" &&
            invocation.TargetMethod.ContainingType.Equals(validationEndpointConventionBuilderExtensions, SymbolEqualityComparer.Default);

    private static IOperation? GetInvocationReceiver(IInvocationOperation invocation)
    {
        if (invocation.Instance is not null)
        {
            return invocation.Instance;
        }

        // For a reduced extension method invocation (e.g. builder.MapPost(...)), the receiver is
        // passed as the first argument rather than exposed via Instance.
        if (invocation.TargetMethod.IsExtensionMethod && !invocation.Arguments.IsEmpty)
        {
            return invocation.Arguments[0].Value;
        }

        return null;
    }
}
