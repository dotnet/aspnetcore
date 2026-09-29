// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

/// <summary>
/// Warns once per server-side Razor Components application that still uses
/// exception-driven navigation during static rendering.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NavigateToReturnAnalyzer : DiagnosticAnalyzer
{
    private const string RazorComponentsEndpointBuilderTypeName = "Microsoft.AspNetCore.Builder.RazorComponentsEndpointRouteBuilderExtensions";
    private const string RazorComponentsServiceBuilderTypeName = "Microsoft.Extensions.DependencyInjection.RazorComponentsServiceCollectionExtensions";
    private const string MapRazorComponentsMethodName = "MapRazorComponents";
    private const string AddRazorComponentsMethodName = "AddRazorComponents";
    private const string DisableThrowNavigationExceptionProperty = "build_property.BlazorDisableThrowNavigationException";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.ExceptionDrivenNavigationIsEnabled);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var outputKind = compilationContext.Compilation.Options.OutputKind;
            if (outputKind != OutputKind.ConsoleApplication && outputKind != OutputKind.WindowsApplication)
            {
                return;
            }

            var sourceTree = compilationContext.Compilation.SyntaxTrees.FirstOrDefault();
            if (sourceTree is not null &&
                compilationContext.Options.AnalyzerConfigOptionsProvider.GetOptions(sourceTree).TryGetValue(
                DisableThrowNavigationExceptionProperty, out var propertyValue) &&
                bool.TryParse(propertyValue, out var disableThrowNavigationException) &&
                disableThrowNavigationException)
            {
                return;
            }

            var endpointBuilderType = compilationContext.Compilation.GetTypeByMetadataName(RazorComponentsEndpointBuilderTypeName);
            var serviceBuilderType = compilationContext.Compilation.GetTypeByMetadataName(RazorComponentsServiceBuilderTypeName);
            if (endpointBuilderType is null && serviceBuilderType is null)
            {
                return;
            }

            var hasServerRendering = 0;
            compilationContext.RegisterOperationAction(operationContext =>
            {
                var invocation = (IInvocationOperation)operationContext.Operation;
                if ((invocation.TargetMethod.Name == MapRazorComponentsMethodName &&
                    endpointBuilderType is not null &&
                    SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, endpointBuilderType)) ||
                    (invocation.TargetMethod.Name == AddRazorComponentsMethodName &&
                    serviceBuilderType is not null &&
                    SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, serviceBuilderType)))
                {
                    Interlocked.Exchange(ref hasServerRendering, 1);
                }
            }, OperationKind.Invocation);

            compilationContext.RegisterCompilationEndAction(compilationEndContext =>
            {
                if (Volatile.Read(ref hasServerRendering) != 0)
                {
                    compilationEndContext.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.ExceptionDrivenNavigationIsEnabled,
                        Location.None));
                }
            });
        });
    }
}
