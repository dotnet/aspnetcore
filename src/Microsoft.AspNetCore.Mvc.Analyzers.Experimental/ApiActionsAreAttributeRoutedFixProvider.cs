// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    [Shared]
    public class ApiActionsAreAttributeRoutedFixProvider : CodeFixProvider
    {
        private static readonly RouteAttributeInfo[] RouteAttributes = new[]
        {
            new RouteAttributeInfo("HttpGet", TypeNames.HttpGetAttribute, new[] { "Get", "Find" }),
            new RouteAttributeInfo("HttpPost", TypeNames.HttpPostAttribute, new[] { "Post", "Create", "Update" }),
            new RouteAttributeInfo("HttpDelete", TypeNames.HttpDeleteAttribute, new[] { "Delete", "Remove" }),
            new RouteAttributeInfo("HttpPut", TypeNames.HttpPutAttribute, new[] { "Put", "Create", "Update" }),
        };

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticDescriptors.MVC7000_ApiActionsMustBeAttributeRouted.Id);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (context.Diagnostics.Length == 0)
            {
                return;
            }

            var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            Debug.Assert(context.Diagnostics.Length == 1);
            var diagnostic = context.Diagnostics[0];
            var methodName = diagnostic.Properties[ApiActionsAreAttributeRoutedAnalyzer.MethodNameKey];

            var matchedByKeyword = false;
            foreach (var routeInfo in RouteAttributes)
            {
                foreach (var keyword in routeInfo.KeyWords)
                {
                    // Determine if the method starts with a conventional key and only show relevant routes.
                    // For e.g. FindPetByCategory would result in HttpGet attribute.
                    if (methodName.StartsWith(keyword, StringComparison.Ordinal))
                    {
                        matchedByKeyword = true;

                        var title = $"Add {routeInfo.Name} attribute";
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title,
                                createChangedDocument: cancellationToken => CreateChangedDocumentAsync(routeInfo.Type, cancellationToken),
                                equivalenceKey: title),
                            context.Diagnostics);
                    }
                }
            }

            if (!matchedByKeyword)
            {
                foreach (var routeInfo in RouteAttributes)
                {
                    var title = $"Add {routeInfo.Name} attribute";
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title,
                            createChangedDocument: cancellationToken => CreateChangedDocumentAsync(routeInfo.Type, cancellationToken),
                            equivalenceKey: title),
                        context.Diagnostics);
                }
            }

            async Task<Document> CreateChangedDocumentAsync(string attributeName, CancellationToken cancellationToken)
            {
                var methodNode = (MethodDeclarationSyntax)rootNode.FindNode(context.Span);

                var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
                var compilation = editor.SemanticModel.Compilation;
                var attributeMetadata = compilation.GetTypeByMetadataName(attributeName);
                var fromRouteAttribute = compilation.GetTypeByMetadataName(TypeNames.FromRouteAttribute);

                attributeName = attributeMetadata.ToMinimalDisplayString(editor.SemanticModel, methodNode.SpanStart);

                // Remove the Attribute suffix from type names e.g. "HttpGetAttribute" -> "HttpGet"
                if (attributeName.EndsWith("Attribute", StringComparison.Ordinal))
                {
                    attributeName = attributeName.Substring(0, attributeName.Length - "Attribute".Length);
                }

                var method = editor.SemanticModel.GetDeclaredSymbol(methodNode);

                var attribute = SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName(attributeName));

                var route = GetRoute(fromRouteAttribute, method);
                if (!string.IsNullOrEmpty(route))
                {
                    attribute = attribute.AddArgumentListArguments(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(route))));
                }

                editor.AddAttribute(methodNode, attribute);
                return editor.GetChangedDocument();
            }
        }

        private static string GetRoute(ITypeSymbol fromRouteAttribute, IMethodSymbol method)
        {
            StringBuilder routeNameBuilder = null;

            foreach (var parameter in method.Parameters)
            {
                if (IsIdParameter(parameter.Name) || parameter.HasAttribute(fromRouteAttribute))
                {
                    if (routeNameBuilder == null)
                    {
                        routeNameBuilder = new StringBuilder(parameter.Name.Length + 2);
                    }
                    else
                    {
                        routeNameBuilder.Append("/");
                    }

                    routeNameBuilder
                        .Append("{")
                        .Append(parameter.Name)
                        .Append("}");
                }
            }

            return routeNameBuilder?.ToString();
        }

        private static bool IsIdParameter(string name)
        {
            // Check if the parameter is named "id" (e.g. int id) or ends in Id (e.g. personId)
            if (name == null || name.Length < 2)
            {
                return false;
            }

            if (string.Equals("id", name, StringComparison.Ordinal))
            {
                return true;
            }

            if (name.Length > 3 && name.EndsWith("Id", StringComparison.Ordinal) && char.IsLower(name[name.Length - 3]))
            {
                return true;
            }

            return false;
        }

        private struct RouteAttributeInfo
        {
            public RouteAttributeInfo(string name, string type, string[] keywords)
            {
                Name = name;
                Type = type;
                KeyWords = keywords;
            }

            public string Name { get; }

            public string Type { get; }

            public string[] KeyWords { get; }
        }
    }
}
