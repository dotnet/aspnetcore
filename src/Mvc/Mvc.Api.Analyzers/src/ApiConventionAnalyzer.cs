// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Mvc.Api.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ApiConventionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode,
        ApiDiagnosticDescriptors.API1001_ActionReturnsUndocumentedSuccessResult,
        ApiDiagnosticDescriptors.API1002_ActionDoesNotReturnDocumentedStatusCode);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
        {
            if (!ApiControllerSymbolCache.TryCreate(compilationStartAnalysisContext.Compilation, out var symbolCache))
            {
                // No-op if we can't find types we care about.
                return;
            }

            InitializeWorker(compilationStartAnalysisContext, symbolCache);
        });
    }

    private static void InitializeWorker(CompilationStartAnalysisContext compilationStartAnalysisContext, ApiControllerSymbolCache symbolCache)
    {
        compilationStartAnalysisContext.RegisterOperationAction(operationStartContext =>
        {
            var method = (IMethodSymbol)operationStartContext.ContainingSymbol;
            if (!ApiControllerFacts.IsApiControllerAction(symbolCache, method))
            {
                return;
            }

            var declaredResponseMetadata = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method);
            var hasUnreadableStatusCodes = !ActualApiResponseMetadataFactory.TryGetActualResponseMetadata(symbolCache, (IMethodBodyOperation)operationStartContext.Operation, operationStartContext.CancellationToken, out var actualResponseMetadata);

            var hasUndocumentedStatusCodes = false;
            foreach (var actualMetadata in actualResponseMetadata)
            {
                var location = actualMetadata.ReturnOperation.ReturnedValue.Syntax.GetLocation();

                if (!DeclaredApiResponseMetadata.Contains(declaredResponseMetadata, actualMetadata))
                {
                    hasUndocumentedStatusCodes = true;
                    if (actualMetadata.IsDefaultResponse)
                    {
                        operationStartContext.ReportDiagnostic(Diagnostic.Create(
                            ApiDiagnosticDescriptors.API1001_ActionReturnsUndocumentedSuccessResult,
                            location));
                    }
                    else
                    {
                        operationStartContext.ReportDiagnostic(Diagnostic.Create(
                            ApiDiagnosticDescriptors.API1000_ActionReturnsUndocumentedStatusCode,
                            location,
                               actualMetadata.StatusCode));
                    }
                }
            }

            if (hasUndocumentedStatusCodes || hasUnreadableStatusCodes)
            {
                // If we produced analyzer warnings about undocumented status codes, don't attempt to determine
                // if there are documented status codes that are missing from the method body.
                return;
            }

            for (var i = 0; i < declaredResponseMetadata.Count; i++)
            {
                var declaredMetadata = declaredResponseMetadata[i];
                if (!Contains(actualResponseMetadata, declaredMetadata))
                {
                    operationStartContext.ReportDiagnostic(Diagnostic.Create(
                        ApiDiagnosticDescriptors.API1002_ActionDoesNotReturnDocumentedStatusCode,
                        method.Locations[0],
                        declaredMetadata.StatusCode));
                }
            }
        }, OperationKind.MethodBody);
    }

    internal static bool Contains(IList<ActualApiResponseMetadata> actualResponseMetadata, DeclaredApiResponseMetadata declaredMetadata)
    {
        for (var i = 0; i < actualResponseMetadata.Count; i++)
        {
            if (declaredMetadata.Matches(actualResponseMetadata[i]))
            {
                return true;
            }
        }

        return false;
    }
}
