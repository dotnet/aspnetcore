// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentDisposableSanityAnalyzer : DiagnosticAnalyzer
{
    private const string IAsyncDisposableTypeName = "System.IAsyncDisposable";
    private const string ValueTaskTypeName = "System.Threading.Tasks.ValueTask";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.ComponentHasDisposeWithoutIDisposable,
            DiagnosticDescriptors.ComponentHasDisposeAsyncWithoutIAsyncDisposable);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var iComponentType = context.Compilation.GetTypeByMetadataName(ComponentsApi.IComponent.MetadataName);
            if (iComponentType is null)
            {
                return;
            }

            var iDisposableType = context.Compilation.GetSpecialType(SpecialType.System_IDisposable);
            var iAsyncDisposableType = context.Compilation.GetTypeByMetadataName(IAsyncDisposableTypeName);
            var valueTaskType = context.Compilation.GetTypeByMetadataName(ValueTaskTypeName);

            context.RegisterSymbolAction(context =>
            {
                var type = (INamedTypeSymbol)context.Symbol;

                if (type.TypeKind != TypeKind.Class || !type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, iComponentType)))
                {
                    return;
                }

                var implementsIDisposable = type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, iDisposableType));
                var implementsIAsyncDisposable = iAsyncDisposableType is not null
                    && type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, iAsyncDisposableType));

                if (implementsIDisposable && implementsIAsyncDisposable)
                {
                    return;
                }

                foreach (var member in type.GetMembers())
                {
                    if (member is not IMethodSymbol { MethodKind: MethodKind.Ordinary, DeclaredAccessibility: Accessibility.Public, IsStatic: false } method
                        || !method.Parameters.IsEmpty)
                    {
                        continue;
                    }

                    if (iDisposableType is not null
                        && !implementsIDisposable
                        && method.Name == "Dispose"
                        && method.ReturnType.SpecialType == SpecialType.System_Void)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ComponentHasDisposeWithoutIDisposable,
                            method.Locations[0],
                            type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                    }
                    else if (iAsyncDisposableType is not null
                        && !implementsIAsyncDisposable
                        && method.Name == "DisposeAsync"
                        && valueTaskType is not null
                        && SymbolEqualityComparer.Default.Equals(method.ReturnType, valueTaskType))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ComponentHasDisposeAsyncWithoutIAsyncDisposable,
                            method.Locations[0],
                            type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)));
                    }
                }
            }, SymbolKind.NamedType);
        });
    }
}
