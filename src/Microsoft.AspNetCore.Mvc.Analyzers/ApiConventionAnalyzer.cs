// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        private static readonly Func<SyntaxNode, bool> _shouldDescendIntoChildren = ShouldDescendIntoChildren;

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
                var methodSyntax = (MethodDeclarationSyntax)syntaxNodeContext.Node;
                var semanticModel = syntaxNodeContext.SemanticModel;
                var method = semanticModel.GetDeclaredSymbol(methodSyntax, syntaxNodeContext.CancellationToken);

                if (!ShouldEvaluateMethod(symbolCache, method))
                {
                    return;
                }

                var conventionAttributes = GetConventionTypeAttributes(symbolCache, method);
                var expectedResponseMetadata = SymbolApiResponseMetadataProvider.GetResponseMetadata(symbolCache, method, conventionAttributes);
                var actualResponseMetadata = new HashSet<int>();

                var context = new ApiConventionContext(
                    symbolCache,
                    syntaxNodeContext,
                    expectedResponseMetadata,
                    actualResponseMetadata);

                var hasUndocumentedStatusCodes = false;
                foreach (var returnStatementSyntax in methodSyntax.DescendantNodes(_shouldDescendIntoChildren).OfType<ReturnStatementSyntax>())
                {
                    hasUndocumentedStatusCodes |= VisitReturnStatementSyntax(context, returnStatementSyntax);
                }

                if (hasUndocumentedStatusCodes)
                {
                    // If we produced analyzer warnings about undocumented status codes, don't attempt to determine
                    // if there are documented status codes that are missing from the method body.
                    return;
                }

                for (var i = 0; i < expectedResponseMetadata.Count; i++)
                {
                    var expectedStatusCode = expectedResponseMetadata[i].StatusCode;
                    if (!actualResponseMetadata.Contains(expectedStatusCode))
                    {
                        context.SyntaxNodeContext.ReportDiagnostic(Diagnostic.Create(
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

        // Returns true if the return statement returns an undocumented status code.
        private static bool VisitReturnStatementSyntax(
            in ApiConventionContext context,
            ReturnStatementSyntax returnStatementSyntax)
        {
            var returnExpression = returnStatementSyntax.Expression;
            if (returnExpression.IsMissing)
            {
                return false;
            }

            var syntaxNodeContext = context.SyntaxNodeContext;

            var typeInfo = syntaxNodeContext.SemanticModel.GetTypeInfo(returnExpression, syntaxNodeContext.CancellationToken);
            if (typeInfo.Type.TypeKind == TypeKind.Error)
            {
                return false;
            }

            var location = returnStatementSyntax.GetLocation();
            var diagnostic = InspectReturnExpression(context, typeInfo.Type, location);
            if (diagnostic != null)
            {
                context.SyntaxNodeContext.ReportDiagnostic(diagnostic);
                return true;
            }

            return false;
        }

        internal static Diagnostic InspectReturnExpression(in ApiConventionContext context, ITypeSymbol type, Location location)
        {
            var defaultStatusCodeAttribute = type
                .GetAttributes(context.SymbolCache.DefaultStatusCodeAttribute, inherit: true)
                .FirstOrDefault();

            if (defaultStatusCodeAttribute != null)
            {
                var statusCode = GetDefaultStatusCode(defaultStatusCodeAttribute);
                if (statusCode == null)
                {
                    // Unable to read the status code. Treat this as valid.
                    return null;
                }

                context.ActualResponseMetadata.Add(statusCode.Value);
                if (!HasStatusCode(context.ExpectedResponseMetadata, statusCode.Value))
                {
                    return Diagnostic.Create(
                        DiagnosticDescriptors.MVC1004_ActionReturnsUndocumentedStatusCode,
                        location,
                        statusCode);
                }
            }
            else if (!context.SymbolCache.IActionResult.IsAssignableFrom(type))
            {
                if (!HasStatusCode(context.ExpectedResponseMetadata, 200) && !HasStatusCode(context.ExpectedResponseMetadata, 201))
                {
                    return Diagnostic.Create(
                        DiagnosticDescriptors.MVC1005_ActionReturnsUndocumentedSuccessResult,
                        location);
                }
            }

            return null;
        }

        internal static int? GetDefaultStatusCode(AttributeData attribute)
        {
            if (attribute != null &&
                attribute.ConstructorArguments.Length == 1 &&
                attribute.ConstructorArguments[0].Kind == TypedConstantKind.Primitive &&
                attribute.ConstructorArguments[0].Value is int statusCode)
            {
                return statusCode;
            }

            return null;
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

        internal static bool HasStatusCode(IList<ApiResponseMetadata> declaredApiResponseMetadata, int statusCode)
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

        private static bool ShouldDescendIntoChildren(SyntaxNode syntaxNode)
        {
            return !syntaxNode.IsKind(SyntaxKind.LocalFunctionStatement) &&
                !syntaxNode.IsKind(SyntaxKind.ParenthesizedLambdaExpression) &&
                !syntaxNode.IsKind(SyntaxKind.SimpleLambdaExpression) &&
                !syntaxNode.IsKind(SyntaxKind.AnonymousMethodExpression);
        }


        internal readonly struct ApiConventionContext
        {
            public ApiConventionContext(
                ApiControllerSymbolCache symbolCache,
                SyntaxNodeAnalysisContext syntaxNodeContext,
                IList<ApiResponseMetadata> expectedResponseMetadata,
                HashSet<int> actualResponseMetadata,
                Action<Diagnostic> reportDiagnostic = null)
            {
                SymbolCache = symbolCache;
                SyntaxNodeContext = syntaxNodeContext;
                ExpectedResponseMetadata = expectedResponseMetadata;
                ActualResponseMetadata = actualResponseMetadata;
                ReportDiagnosticAction = reportDiagnostic;
            }

            public ApiControllerSymbolCache SymbolCache { get; }
            public SyntaxNodeAnalysisContext SyntaxNodeContext { get; }
            public IList<ApiResponseMetadata> ExpectedResponseMetadata { get; }
            public HashSet<int> ActualResponseMetadata { get; }
            private Action<Diagnostic> ReportDiagnosticAction { get; }

            public void ReportDiagnostic(Diagnostic diagnostic)
            {
                if (ReportDiagnosticAction != null)
                {
                    ReportDiagnosticAction(diagnostic);
                }

                SyntaxNodeContext.ReportDiagnostic(diagnostic);
            }
        }

    }
}
