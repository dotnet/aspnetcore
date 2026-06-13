// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class JSInvokableAnalyzer : DiagnosticAnalyzer
{
    private const string JSInvokableAttributeTypeName = "Microsoft.JSInterop.JSInvokableAttribute";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
        [
            DiagnosticDescriptors.JSInvokableMethodShouldBePublic,
            DiagnosticDescriptors.JSInvokableMethodShouldBeStatic,
        ]);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.RegisterCompilationStartAction(context =>
        {
            var jsInvokableAttribute = context.Compilation.GetTypeByMetadataName(JSInvokableAttributeTypeName);
            if (jsInvokableAttribute is null)
            {
                // The attribute isn't available
                return;
            }

            context.RegisterSymbolAction(context =>
            {
                var method = (IMethodSymbol)context.Symbol;
                var attributes = method.GetAttributes();
                if (attributes.Length == 0)
                {
                    return;
                }

                var isJsInvokable = attributes.Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, jsInvokableAttribute));
                if (!isJsInvokable)
                {
                    return;
                }

                var methodLocation = method.Locations.FirstOrDefault();
                if (methodLocation == null)
                {
                    return;
                }

                if (method.DeclaredAccessibility != Accessibility.Public)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.JSInvokableMethodShouldBePublic,
                        methodLocation,
                        method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                }

                if (!method.IsStatic)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.JSInvokableMethodShouldBeStatic,
                        methodLocation,
                        method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                }
            }, SymbolKind.Method);
        });
    }
}
