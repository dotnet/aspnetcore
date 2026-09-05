// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RazorComponentResultParameterAnalyzer : DiagnosticAnalyzer
{
    public RazorComponentResultParameterAnalyzer()
    {
        SupportedDiagnostics = ImmutableArray.Create(
            DiagnosticDescriptors.RazorComponentResultParameterDoesNotExist);
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            if (!ComponentSymbols.TryCreate(context.Compilation, out var symbols))
            {
                // Types we need are not defined.
                return;
            }

            var razorComponentResultOfT = context.Compilation.GetTypeByMetadataName(ComponentsApi.RazorComponentResultOfT.MetadataName);
            if (razorComponentResultOfT is null)
            {
                // RazorComponentResult<TComponent> is not referenced by this compilation.
                return;
            }

            context.RegisterOperationAction(context =>
            {
                var objectCreation = (IObjectCreationOperation)context.Operation;

                // We only care about constructing RazorComponentResult<TComponent> with a single loosely-typed
                // parameters argument, which is the ergonomic (and error-prone) overload from the issue.
                if (objectCreation.Type is not INamedTypeSymbol createdType ||
                    !SymbolEqualityComparer.Default.Equals(createdType.OriginalDefinition, razorComponentResultOfT))
                {
                    return;
                }

                if (createdType.TypeArguments.Length != 1 ||
                    createdType.TypeArguments[0] is not INamedTypeSymbol componentType)
                {
                    // The component type is an open generic type parameter; we cannot resolve its parameters.
                    return;
                }

                if (objectCreation.Arguments.Length != 1)
                {
                    return;
                }

                if (UnwrapConversions(objectCreation.Arguments[0].Value) is not IAnonymousObjectCreationOperation anonymousObject)
                {
                    // Only anonymous objects (e.g. new { Foo = 1 }) expose statically known parameter names.
                    // Dictionaries and named types are matched by name at runtime and are intentionally ignored.
                    return;
                }

                if (!TryGetComponentParameterNames(symbols, componentType, out var parameterNames))
                {
                    // The component captures unmatched values, so any parameter name is accepted at runtime.
                    return;
                }

                foreach (var initializer in anonymousObject.Initializers)
                {
                    if (initializer is not ISimpleAssignmentOperation { Target: IPropertyReferenceOperation propertyReference })
                    {
                        continue;
                    }

                    var parameterName = propertyReference.Property.Name;
                    if (parameterNames.Contains(parameterName))
                    {
                        continue;
                    }

                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.RazorComponentResultParameterDoesNotExist,
                        GetParameterNameLocation(initializer.Syntax),
                        componentType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                        parameterName));
                }
            }, OperationKind.ObjectCreation);
        });
    }

    private static IOperation UnwrapConversions(IOperation operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    private static bool TryGetComponentParameterNames(
        ComponentSymbols symbols,
        INamedTypeSymbol componentType,
        out HashSet<string> parameterNames)
    {
        parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (INamedTypeSymbol? type = componentType; type is not null; type = type.BaseType)
        {
            foreach (var member in type.GetMembers())
            {
                if (member is not IPropertySymbol property)
                {
                    continue;
                }

                if (ComponentFacts.IsParameterWithCaptureUnmatchedValues(symbols, property))
                {
                    // The component accepts arbitrary parameter names via CaptureUnmatchedValues,
                    // so no name can be considered invalid.
                    return false;
                }

                if (ComponentFacts.IsParameter(symbols, property) &&
                    !ComponentFacts.IsCascadingParameter(symbols, property))
                {
                    parameterNames.Add(property.Name);
                }
            }
        }

        return true;
    }

    private static Location GetParameterNameLocation(SyntaxNode syntax)
    {
        // Point the diagnostic at the member name (e.g. `Foo` in `Foo = 1`) rather than the whole declarator.
        return syntax switch
        {
            AnonymousObjectMemberDeclaratorSyntax { NameEquals: { } nameEquals } => nameEquals.Name.GetLocation(),
            AnonymousObjectMemberDeclaratorSyntax { Expression: IdentifierNameSyntax identifier } => identifier.GetLocation(),
            _ => syntax.GetLocation(),
        };
    }
}
