// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ActionsMustNotBeAsyncVoidAnalyzer : ControllerAnalyzerBase
    {
        public static readonly string ReturnTypeKey = "ReturnType";

        public ActionsMustNotBeAsyncVoidAnalyzer()
            : base(DiagnosticDescriptors.MVC7003_ActionsMustNotBeAsyncVoid)
        {
        }

        protected override void InitializeWorker(ControllerAnalyzerContext analyzerContext)
        {
            analyzerContext.Context.RegisterSyntaxNodeAction(context =>
            {
                var methodSyntax = (MethodDeclarationSyntax)context.Node;
                var method = context.SemanticModel.GetDeclaredSymbol(methodSyntax, context.CancellationToken);

                if (!analyzerContext.IsControllerAction(method))
                {
                    return;
                }

                if (!method.IsAsync || !method.ReturnsVoid)
                {
                    return;
                }

                var returnType = analyzerContext.SystemThreadingTask.ToMinimalDisplayString(
                    context.SemanticModel,
                    methodSyntax.ReturnType.SpanStart);

                var properties = ImmutableDictionary.Create<string, string>(StringComparer.Ordinal)
                    .Add(ReturnTypeKey, returnType);

                var location = methodSyntax.ReturnType.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(
                    SupportedDiagnostic,
                    location,
                    properties: properties));

            }, SyntaxKind.MethodDeclaration);
        }
    }
}
