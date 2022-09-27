// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.Extensions.Internal;

internal sealed class InternalUsageAnalyzer
{
    private readonly Func<ISymbol, bool> _isInternalNamespace;
    private readonly Func<ISymbol, bool> _hasInternalAttribute;
    private readonly DiagnosticDescriptor _descriptor;

    /// <summary>
    /// Creates a new instance of <see cref="InternalUsageAnalyzer" />. The creator should provide delegates to help determine whether
    /// a given symbol is internal or not, and a <see cref="DiagnosticDescriptor" /> to create errors.
    /// </summary>
    /// <param name="isInInternalNamespace">The delegate used to check if a symbol belongs to an internal namespace.</param>
    /// <param name="hasInternalAttribute">The delegate used to check if a symbol has an internal attribute.</param>
    /// <param name="descriptor">
    /// The <see cref="DiagnosticDescriptor" /> used to create errors. The error message should expect a single parameter
    /// used for the display name of the member.
    /// </param>
    public InternalUsageAnalyzer(Func<ISymbol, bool> isInInternalNamespace, Func<ISymbol, bool> hasInternalAttribute, DiagnosticDescriptor descriptor)
    {
        _isInternalNamespace = isInInternalNamespace ?? new Func<ISymbol, bool>((_) => false);
        _hasInternalAttribute = hasInternalAttribute ?? new Func<ISymbol, bool>((_) => false);
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
    }

    public void Register(AnalysisContext context)
    {
        context.EnableConcurrentExecution();

        // Analyze usage of our internal types in method bodies.
        context.RegisterOperationAction(
            AnalyzeOperation,
            OperationKind.ObjectCreation,
            OperationKind.Invocation,
            OperationKind.FieldReference,
            OperationKind.MethodReference,
            OperationKind.PropertyReference,
            OperationKind.EventReference);

        // Analyze declarations that use our internal types in API surface.
        context.RegisterSymbolAction(
            AnalyzeSymbol,
            SymbolKind.NamedType,
            SymbolKind.Field,
            SymbolKind.Method,
            SymbolKind.Property,
            SymbolKind.Event);
    }

    private void AnalyzeOperation(OperationAnalysisContext context)
    {
        var symbol = context.Operation switch
        {
            IObjectCreationOperation creation => creation.Constructor,
            IInvocationOperation invocation => invocation.TargetMethod,
            IFieldReferenceOperation field => field.Member,
            IMethodReferenceOperation method => method.Member,
            IPropertyReferenceOperation property => property.Member,
            IEventReferenceOperation @event => @event.Member,
            _ => throw new InvalidOperationException("Unexpected operation kind: " + context.Operation.Kind),
        };

        VisitOperationSymbol(context, symbol);
    }

    private void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        // Note: we don't currently try to detect second-order usage of these types
        // like public Task<InternalFoo> GetFooAsync() { }.
        //
        // This probably accomplishes our goals OK for now, which are focused on use of these
        // types in method bodies.
        switch (context.Symbol)
        {
            case INamedTypeSymbol type:
                VisitDeclarationSymbol(context, type.BaseType, type);
                foreach (var @interface in type.Interfaces)
                {
                    VisitDeclarationSymbol(context, @interface, type);
                }
                break;

            case IFieldSymbol field:
                VisitDeclarationSymbol(context, field.Type, field);
                break;

            case IMethodSymbol method:

                // Ignore return types on property-getters. Those will be reported through
                // the property analysis.
                if (method.MethodKind != MethodKind.PropertyGet)
                {
                    VisitDeclarationSymbol(context, method.ReturnType, method);
                }

                // Ignore parameters on property-setters. Those will be reported through
                // the property analysis.
                if (method.MethodKind != MethodKind.PropertySet)
                {
                    foreach (var parameter in method.Parameters)
                    {
                        VisitDeclarationSymbol(context, parameter.Type, method);
                    }
                }
                break;

            case IPropertySymbol property:
                VisitDeclarationSymbol(context, property.Type, property);
                break;

            case IEventSymbol @event:
                VisitDeclarationSymbol(context, @event.Type, @event);
                break;
        }
    }

    // Similar logic here to VisitDeclarationSymbol, keep these in sync.
    private void VisitOperationSymbol(OperationAnalysisContext context, ISymbol symbol)
    {
        if (symbol == null || SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, context.Compilation.Assembly))
        {
            // The type is being referenced within the same assembly. This is valid use of an "internal" type
            return;
        }

        if (HasInternalAttribute(symbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                _descriptor,
                context.Operation.Syntax.GetLocation(),
                symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            return;
        }

        var containingType = symbol.ContainingType;
        if (IsInInternalNamespace(containingType) || HasInternalAttribute(containingType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                _descriptor,
                context.Operation.Syntax.GetLocation(),
                containingType.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            return;
        }
    }

    // Similar logic here to VisitOperationSymbol, keep these in sync.
    private void VisitDeclarationSymbol(SymbolAnalysisContext context, ISymbol symbol, ISymbol symbolForDiagnostic)
    {
        if (symbol == null || SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, context.Compilation.Assembly))
        {
            // This is part of the compilation, avoid this analyzer when building from source.
            return;
        }

        if (HasInternalAttribute(symbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                _descriptor,
                symbolForDiagnostic.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation() ?? Location.None,
                symbol.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            return;
        }

        var containingType = symbol as INamedTypeSymbol ?? symbol.ContainingType;
        if (IsInInternalNamespace(containingType) || HasInternalAttribute(containingType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                _descriptor,
                symbolForDiagnostic.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation() ?? Location.None,
                containingType.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            return;
        }
    }

    private bool HasInternalAttribute(ISymbol symbol) => _hasInternalAttribute(symbol);

    private bool IsInInternalNamespace(ISymbol symbol) => _isInternalNamespace(symbol);
}
