// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentParameterAnalyzer : DiagnosticAnalyzer
{
    public ComponentParameterAnalyzer()
    {
        SupportedDiagnostics = ImmutableArray.Create(new[]
        {
            DiagnosticDescriptors.ComponentParametersShouldBePublic,
            DiagnosticDescriptors.ComponentParameterSettersShouldBePublic,
            DiagnosticDescriptors.ComponentParameterCaptureUnmatchedValuesMustBeUnique,
            DiagnosticDescriptors.ComponentParameterCaptureUnmatchedValuesHasWrongType,
            DiagnosticDescriptors.ComponentParametersShouldBeAutoProperties,
        });
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

            // This operates per-type because one of the validations we need has to look for duplicates
            // defined on the same type.
            context.RegisterSymbolStartAction(context =>
            {
                var properties = new List<IPropertySymbol>();

                var type = (INamedTypeSymbol)context.Symbol;
                foreach (var member in type.GetMembers())
                {
                    if (member is IPropertySymbol property && ComponentFacts.IsParameter(symbols, property))
                    {
                        // Annotated with [Parameter]. We ignore [CascadingParameter]'s because they don't interact with tooling and don't currently have any analyzer restrictions.
                        properties.Add(property);
                    }
                }

                if (properties.Count == 0)
                {
                    return;
                }

                context.RegisterSymbolEndAction(context =>
                {
                    var captureUnmatchedValuesParameters = new List<IPropertySymbol>();

                    // Per-property validations
                    foreach (var property in properties)
                    {
                        var propertyLocation = property.Locations.FirstOrDefault();
                        if (propertyLocation == null)
                        {
                            continue;
                        }

                        if (property.DeclaredAccessibility != Accessibility.Public)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ComponentParametersShouldBePublic,
                                propertyLocation,
                                property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                        }
                        else if (property.SetMethod?.DeclaredAccessibility != Accessibility.Public)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ComponentParameterSettersShouldBePublic,
                                propertyLocation,
                                property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                        }

                        if (ComponentFacts.IsParameterWithCaptureUnmatchedValues(symbols, property))
                        {
                            captureUnmatchedValuesParameters.Add(property);

                            // Check the type, we need to be able to assign a Dictionary<string, object>
                            var conversion = context.Compilation.ClassifyConversion(symbols.ParameterCaptureUnmatchedValuesRuntimeType, property.Type);
                            if (!conversion.Exists || conversion.IsExplicit)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(
                                    DiagnosticDescriptors.ComponentParameterCaptureUnmatchedValuesHasWrongType,
                                    propertyLocation,
                                    property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                                    property.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                                    symbols.ParameterCaptureUnmatchedValuesRuntimeType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                            }
                        }
                        if (!IsAutoProperty(property) && !IsSameSemanticAsAutoProperty(property, context.CancellationToken))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.ComponentParametersShouldBeAutoProperties,
                                propertyLocation,
                                property.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                        }
                    }

                    // Check if the type defines multiple CaptureUnmatchedValues parameters. Doing this outside the loop means we place the
                    // errors on the type.
                    if (captureUnmatchedValuesParameters.Count > 1)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ComponentParameterCaptureUnmatchedValuesMustBeUnique,
                            context.Symbol.Locations[0],
                            type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                            Environment.NewLine,
                            string.Join(
                                Environment.NewLine,
                                captureUnmatchedValuesParameters.Select(p => p.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)).OrderBy(n => n))));
                    }
                });
            }, SymbolKind.NamedType);
        });
    }

    /// <summary>
    /// Check if a property is an auto-property.
    /// TODO: Remove this helper when https://github.com/dotnet/roslyn/issues/46682 is handled.
    /// </summary>
    private static bool IsAutoProperty(IPropertySymbol propertySymbol)
       => propertySymbol.ContainingType.GetMembers()
              .OfType<IFieldSymbol>()
              .Any(f => f.IsImplicitlyDeclared && SymbolEqualityComparer.Default.Equals(propertySymbol, f.AssociatedSymbol));

    private static bool IsSameSemanticAsAutoProperty(IPropertySymbol symbol, CancellationToken cancellationToken)
    {
        if (symbol.DeclaringSyntaxReferences.Length == 1 &&
            symbol.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is PropertyDeclarationSyntax syntax &&
            syntax.AccessorList?.Accessors.Count == 2)
        {
            var getterAccessor = syntax.AccessorList.Accessors[0];
            var setterAccessor = syntax.AccessorList.Accessors[1];
            if (getterAccessor.IsKind(SyntaxKind.SetAccessorDeclaration))
            {
                // Swap if necessary.
                (getterAccessor, setterAccessor) = (setterAccessor, getterAccessor);
            }

            if (!getterAccessor.IsKind(SyntaxKind.GetAccessorDeclaration) || !setterAccessor.IsKind(SyntaxKind.SetAccessorDeclaration))
            {
                return false;
            }

            IdentifierNameSyntax? identifierUsedInGetter = GetIdentifierUsedInGetter(getterAccessor);
            if (identifierUsedInGetter is null)
            {
                return false;
            }

            IdentifierNameSyntax? identifierUsedInSetter = GetIdentifierUsedInSetter(setterAccessor);
            return identifierUsedInGetter.Identifier.ValueText == identifierUsedInSetter?.Identifier.ValueText;
        }

        return false;
    }

    private static IdentifierNameSyntax? GetIdentifierUsedInGetter(AccessorDeclarationSyntax getter)
    {
        if (getter.Body is { Statements: { Count: 1 } } && getter.Body.Statements[0] is ReturnStatementSyntax returnStatement)
        {
            return returnStatement.Expression as IdentifierNameSyntax;
        }

        return getter.ExpressionBody?.Expression as IdentifierNameSyntax;
    }

    private static IdentifierNameSyntax? GetIdentifierUsedInSetter(AccessorDeclarationSyntax setter)
    {
        AssignmentExpressionSyntax? assignmentExpression = null;
        if (setter.Body is not null)
        {
            if (setter.Body.Statements.Count == 1)
            {
                assignmentExpression = (setter.Body.Statements[0] as ExpressionStatementSyntax)?.Expression as AssignmentExpressionSyntax;
            }
        }
        else
        {
            assignmentExpression = setter.ExpressionBody?.Expression as AssignmentExpressionSyntax;
        }

        if (assignmentExpression is not null && assignmentExpression.Right is IdentifierNameSyntax right &&
            right.Identifier.ValueText == "value")
        {
            return assignmentExpression.Left as IdentifierNameSyntax;
        }

        return null;
    }
}
