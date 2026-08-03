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
        "ASP0032",
        "[ValidatableType] is applied to an inaccessible type",
        "The type '{0}' is marked with [ValidatableType] but is not accessible from the generated validation code. The type must be public or internal and must not be a file-local type, otherwise its validation is silently skipped.",
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: null,
        helpLinkUri: GetHelpLinkUri("ASP0032"));

    internal static readonly DiagnosticDescriptor EndpointParameterTypeIsNotAccessible = new(
        "ASP0033",
        "Endpoint parameter type is inaccessible from generated code",
        "The endpoint parameter '{0}' is not accessible from the generated validation code. The type must be public or internal and must not be a file-local type, otherwise its validation is silently skipped.",
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: null,
        helpLinkUri: GetHelpLinkUri("ASP0033"),
        WellKnownDiagnosticTags.CompilationEnd);

    internal static readonly DiagnosticDescriptor ValidatablePropertyIsNotAccessible = new(
        "ASP0034",
        "Validatable property or its type is not accessible",
        "The property '{0}' on type '{1}' declares validation but is not public, so it is silently skipped by the validation source generator. Make the property and its getter public for it to be validated.",
        Usage,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: null,
        helpLinkUri: GetHelpLinkUri("ASP0034"));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [
            ValidatableTypeIsNotAccessible,
            EndpointParameterTypeIsNotAccessible,
            ValidatablePropertyIsNotAccessible,
        ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static bool IsInaccessibleFromGeneratedCode(ITypeSymbol type)
        => type is INamedTypeSymbol { IsFileLocal: true } ||
            type.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal);

    private static void AnalyzeType(
        Action<Diagnostic> reportDiagnostic,
        INamedTypeSymbol skipValidationAttributeSymbol,
        ITypeSymbol currentType,
        ConcurrentDictionary<ITypeSymbol, byte> allValidatableTypes)
    {
        if (!allValidatableTypes.TryAdd(currentType, 0))
        {
            return;
        }

        foreach (var property in currentType.GetMembers().OfType<IPropertySymbol>())
        {
            if (property.IsStatic || property.IsIndexer)
            {
                continue;
            }

            if (property.IsSkippedValidationProperty(skipValidationAttributeSymbol))
            {
                continue;
            }

            if (property.DeclaredAccessibility is not Accessibility.Public ||
                IsInaccessibleFromGeneratedCode(property.Type))
            {
                reportDiagnostic(Diagnostic.Create(
                    ValidatablePropertyIsNotAccessible,
                    property.Locations.FirstOrDefault(),
                    property.Name,
                    currentType.ToDisplayString()));
            }
        }
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var compilation = context.Compilation;
        var serviceCollectionExtensionsType = compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ValidationServiceCollectionExtensions");
        var validatableTypeAttribute = compilation.GetTypeByMetadataName("Microsoft.Extensions.Validation.ValidatableTypeAttribute");

        var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);
        var fromServiceMetadata = wellKnownTypes.GetOptional(WellKnownTypeData.WellKnownType.Microsoft_AspNetCore_Http_Metadata_IFromServiceMetadata);
        var fromKeyedServiceAttribute = wellKnownTypes.GetOptional(WellKnownTypeData.WellKnownType.Microsoft_Extensions_DependencyInjection_FromKeyedServicesAttribute);
        var skipValidationAttribute = wellKnownTypes.Get(WellKnownTypeData.WellKnownType.Microsoft_Extensions_Validation_SkipValidationAttribute);

        var allValidatableTypes = new ConcurrentDictionary<ITypeSymbol, byte>(SymbolEqualityComparer.Default);
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

                    AnalyzeType(context.ReportDiagnostic, skipValidationAttribute, attributedType, allValidatableTypes);
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
            if (invocation.TryGetRouteHandlerMethod(semanticModel, needsAccurateSignature: false, out var routeHandlerMethod))
            {
                foreach (var parameter in routeHandlerMethod.Parameters)
                {
                    if (parameter.IsServiceParameter(fromServiceMetadata, fromKeyedServiceAttribute)
                        || parameter.IsSkippedValidationParameter(skipValidationAttribute))
                    {
                        continue;
                    }

                    endpointParameters.TryAdd(parameter, 0);
                }
            }
        }, OperationKind.Invocation);

        // Candidate types for the "not in graph" diagnostic.
        var reachableAtRuntimeCandidates = new ConcurrentQueue<INamedTypeSymbol>();

        context.RegisterSymbolAction(context =>
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (IsPossiblyReachableAtRuntimeInValidatableTypeGraph(type))
            {
                reachableAtRuntimeCandidates.Enqueue(type);
            }
        }, SymbolKind.NamedType);

        context.RegisterCompilationEndAction(context =>
        {
            if (!addValidationFound && validatableTypeAttributeFound)
            {
                // TODO: Report diagnostic
                return;
            }

            if (addValidationFound)
            {
                foreach (var parameter in endpointParameters.Keys)
                {
                    if (IsInaccessibleFromGeneratedCode(parameter.Type))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(EndpointParameterTypeIsNotAccessible, parameter.Locations.FirstOrDefault(), parameter.Name));
                        continue;
                    }

                    AnalyzeType(context.ReportDiagnostic, skipValidationAttribute, parameter.Type, allValidatableTypes);
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

    private static bool IsPossiblyReachableAtRuntimeInValidatableTypeGraph(INamedTypeSymbol type)
    {
        // The types we are looking for here are concrete instantiatable types which can be hidden
        // behind an interface or base type.
        // Consider:
        //
        // class Base { } // could be an interface as well.
        //
        // class Derived : Base
        // {
        //     [MyValidation]
        //     public string S { get; set; }
        // }
        //
        // [ValidatableType]
        // class ValidatableModel
        // {
        //     public Base B { get; set; }
        // }
        //
        // In the above case, we want to keep track of "Derived" types.
        // At the end of compilation, if we find any such types which are not
        // part of the validatable type graph, and the base type or interface is part of the graph,
        // then we want to report a diagnostic.
        // Only non-abstract and non-static types can be instantiated behind an interface or base type.
        if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct)
            || type.IsStatic
            || type.IsAbstract)
        {
            return false;
        }

        // Must implement an interface or extend a non-object base type to be reachable through the graph.
        var hasBaseType = type.BaseType is { SpecialType: not SpecialType.System_Object and not SpecialType.System_ValueType };
        if (!hasBaseType && !type.AllInterfaces.Any())
        {
            return false;
        }

        return true;
    }
}
