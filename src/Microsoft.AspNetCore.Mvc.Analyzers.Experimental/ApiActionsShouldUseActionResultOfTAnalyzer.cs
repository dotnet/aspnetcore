// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ApiActionsShouldUseActionResultOfTAnalyzer : ApiControllerAnalyzerBase
    {
        public static readonly string ReturnTypeKey = "ReturnType";

        public ApiActionsShouldUseActionResultOfTAnalyzer()
            : base(DiagnosticDescriptors.MVC7002_ApiActionsShouldReturnActionResultOf)
        {
        }

        protected override void InitializeWorker(ApiControllerAnalyzerContext analyzerContext)
        {
            analyzerContext.Context.RegisterSyntaxNodeAction(context =>
            {
                var methodSyntax = (MethodDeclarationSyntax)context.Node;
                if (methodSyntax.Body == null)
                {
                    // Ignore expression bodied methods.
                }

                var method = context.SemanticModel.GetDeclaredSymbol(methodSyntax, context.CancellationToken);
                if (!analyzerContext.IsApiAction(method))
                {
                    return;
                }

                if (method.ReturnsVoid || method.ReturnType.Kind != SymbolKind.NamedType)
                {
                    return;
                }

                var declaredReturnType = method.ReturnType;
                var namedReturnType = (INamedTypeSymbol)method.ReturnType;
                var isTaskOActionResult = false;
                if (namedReturnType.ConstructedFrom?.IsAssignableFrom(analyzerContext.SystemThreadingTaskOfT) ?? false)
                {
                    // Unwrap Task<T>.
                    isTaskOActionResult = true;
                    declaredReturnType = namedReturnType.TypeArguments[0];
                }

                if (!declaredReturnType.IsAssignableFrom(analyzerContext.IActionResult))
                {
                    // Method signature does not look like IActionResult MyAction or SomeAwaitable<IActionResult>.
                    // Nothing to do here.
                    return;
                }

                // Method returns an IActionResult. Determine if the method block returns an ObjectResult
                foreach (var returnStatement in methodSyntax.DescendantNodes().OfType<ReturnStatementSyntax>())
                {
                    var returnType = context.SemanticModel.GetTypeInfo(returnStatement.Expression, context.CancellationToken);
                    if (returnType.Type == null || returnType.Type.Kind == SymbolKind.ErrorType)
                    {
                        continue;
                    }

                    ImmutableDictionary<string, string> properties = null;
                    if (returnType.Type.IsAssignableFrom(analyzerContext.ObjectResult))
                    {
                        // Check if the method signature looks like "return Ok(userModelInstance)". If so, we can infer the type of userModelInstance
                        if (returnStatement.Expression is InvocationExpressionSyntax invocation &&
                            invocation.ArgumentList.Arguments.Count == 1)
                        {
                            var typeInfo = context.SemanticModel.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression);
                            var desiredReturnType = analyzerContext.ActionResultOfT.Construct(typeInfo.Type);
                            if (isTaskOActionResult)
                            {
                                desiredReturnType = analyzerContext.SystemThreadingTaskOfT.Construct(desiredReturnType);
                            }

                            var desiredReturnTypeString = desiredReturnType.ToMinimalDisplayString(
                                context.SemanticModel,
                                methodSyntax.ReturnType.SpanStart);

                            properties = ImmutableDictionary.Create<string, string>(StringComparer.Ordinal)
                                .Add(ReturnTypeKey, desiredReturnTypeString);
                        }

                        context.ReportDiagnostic(Diagnostic.Create(
                            SupportedDiagnostic,
                            methodSyntax.ReturnType.GetLocation(),
                            properties: properties));
                    }
                }
            }, SyntaxKind.MethodDeclaration);
        }
    }
}
