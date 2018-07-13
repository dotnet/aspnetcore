// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ApiConventionAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode,
            DiagnosticDescriptors.MVC1005_ActionReturnsUndocumentedSuccessResult,
            DiagnosticDescriptors.MVC1006_ActionDoesNotReturnDocumentedStatusCode);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
            {
                var symbolCache = new ApiControllerSymbolCache(compilationStartAnalysisContext.Compilation);
                if (symbolCache.ApiConventionTypeAttribute == null || symbolCache.ApiConventionTypeAttribute.TypeKind == TypeKind.Error)
                {
                    // No-op if we can't find types we care about.
                    return;
                }

                InitializeWorker(compilationStartAnalysisContext, symbolCache);
            });
        }

        private void InitializeWorker(CompilationStartAnalysisContext compilationStartAnalysisContext, ApiControllerSymbolCache symbolCache)
        {
            compilationStartAnalysisContext.RegisterSyntaxNodeAction(syntaxNodeContext =>
            {
                var cancellationToken = syntaxNodeContext.CancellationToken;
                var methodSyntax = (MethodDeclarationSyntax)syntaxNodeContext.Node;
                var semanticModel = syntaxNodeContext.SemanticModel;
                var method = semanticModel.GetDeclaredSymbol(methodSyntax, syntaxNodeContext.CancellationToken);

                if (!ShouldEvaluateMethod(symbolCache, method))
                {
                    return;
                }

                var conventionAttributes = GetConventionTypeAttributes(symbolCache, method);
                var declaredResponseMetadata = SymbolApiResponseMetadataProvider.GetDeclaredResponseMetadata(symbolCache, method, conventionAttributes);

                var hasUnreadableStatusCodes = SymbolApiResponseMetadataProvider.TryGetActualResponseMetadata(symbolCache, semanticModel, methodSyntax, cancellationToken, out var actualResponseMetadata);
                var hasUndocumentedStatusCodes = false;
                foreach (var item in actualResponseMetadata)
                {
                    var location = item.ReturnStatement.GetLocation();

                    if (item.IsDefaultResponse)
                    {
                        if (!(HasStatusCode(declaredResponseMetadata, 200) || HasStatusCode(declaredResponseMetadata, 201)))
                        {
                            hasUndocumentedStatusCodes = true;
                            syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.MVC1005_ActionReturnsUndocumentedSuccessResult,
                                location));
                        }
                    }
                    else if (!HasStatusCode(declaredResponseMetadata, item.StatusCode))
                    {
                        hasUndocumentedStatusCodes = true;
                        syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode,
                            location,
                            item.StatusCode));
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
                    var expectedStatusCode = declaredResponseMetadata[i].StatusCode;
                    if (!HasStatusCode(actualResponseMetadata, expectedStatusCode))
                    {
                        syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.MVC1006_ActionDoesNotReturnDocumentedStatusCode,
                            methodSyntax.Identifier.GetLocation(),
                            expectedStatusCode));
                    }
                }

            }, SyntaxKind.MethodDeclaration);
        }

        internal IReadOnlyList<AttributeData> GetConventionTypeAttributes(ApiControllerSymbolCache symbolCache, IMethodSymbol method)
        {
            var attributes = method.ContainingType.GetAttributes(symbolCache.ApiConventionTypeAttribute).ToArray();
            if (attributes.Length == 0)
            {
                attributes = method.ContainingAssembly.GetAttributes(symbolCache.ApiConventionTypeAttribute).ToArray();
            }

            return attributes;
        }

        internal static bool ShouldEvaluateMethod(ApiControllerSymbolCache symbolCache, IMethodSymbol method)
        {
            if (method == null)
            {
                return false;
            }

            if (method.ReturnsVoid || method.ReturnType.TypeKind == TypeKind.Error)
            {
                return false;
            }

            if (!MvcFacts.IsController(method.ContainingType, symbolCache.ControllerAttribute, symbolCache.NonControllerAttribute))
            {
                return false;
            }

            if (!method.ContainingType.HasAttribute(symbolCache.IApiBehaviorMetadata, inherit: true))
            {
                return false;
            }

            if (!MvcFacts.IsControllerAction(method, symbolCache.NonActionAttribute, symbolCache.IDisposableDispose))
            {
                return false;
            }

            return true;
        }

        internal static bool HasStatusCode(IList<DeclaredApiResponseMetadata> declaredApiResponseMetadata, int statusCode)
        {
            if (declaredApiResponseMetadata.Count == 0)
            {
                // When no status code is declared, a 200 OK is implied.
                return statusCode == 200;
            }

            for (var i = 0; i < declaredApiResponseMetadata.Count; i++)
            {
                if (declaredApiResponseMetadata[i].StatusCode == statusCode)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool HasStatusCode(IList<ActualApiResponseMetadata> actualResponseMetadata, int statusCode)
        {
            for (var i = 0; i < actualResponseMetadata.Count; i++)
            {
                if (actualResponseMetadata[i].IsDefaultResponse)
                {
                    return statusCode == 200 || statusCode == 201;
                }

                else if(actualResponseMetadata[i].StatusCode == statusCode)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
