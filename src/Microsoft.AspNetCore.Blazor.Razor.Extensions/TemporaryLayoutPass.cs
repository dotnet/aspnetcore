// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// This code is temporary. It finds top-level expressions of the form
    ///     @Layout<SomeType>()
    /// ... and converts them into [Layout(typeof(SomeType))] attributes on the class.
    /// Once we're able to add Blazor-specific directives and have them show up in tooling,
    /// we'll replace this with a simpler and cleaner "@Layout SomeType" directive.
    /// </summary>
    internal class TemporaryLayoutPass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
    {
        // Example: "Layout<MyApp.Namespace.SomeType<T1, T2>>()"
        // Captures: MyApp.Namespace.SomeType<T1, T2>
        private static readonly Regex LayoutSourceRegex
            = new Regex(@"^\s*Layout\s*<(.+)\>\s*\(\s*\)\s*$");
        private const string LayoutAttributeTypeName
            = "Microsoft.AspNetCore.Blazor.Layouts.LayoutAttribute";

        public static void Register(IRazorEngineBuilder configuration)
        {
            configuration.Features.Add(new TemporaryLayoutPass());
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var visitor = new Visitor();
            visitor.Visit(documentNode);

            if (visitor.DidFindLayoutDeclaration)
            {
                visitor.MethodNode.Children.Remove(visitor.LayoutNode);

                var attributeNode = new CSharpCodeIntermediateNode();
                attributeNode.Children.Add(new IntermediateToken()
                {
                    Kind = TokenKind.CSharp,
                    Content = $"[{LayoutAttributeTypeName}(typeof ({visitor.LayoutType}))]" + Environment.NewLine,
                });

                var classNodeIndex = visitor
                    .NamespaceNode
                    .Children
                    .IndexOf(visitor.ClassNode);
                visitor.NamespaceNode.Children.Insert(classNodeIndex, attributeNode);
            }
        }

        private class Visitor : IntermediateNodeWalker
        {
            public bool DidFindLayoutDeclaration { get; private set; }
            public NamespaceDeclarationIntermediateNode NamespaceNode { get; private set; }
            public ClassDeclarationIntermediateNode ClassNode { get; private set; }
            public MethodDeclarationIntermediateNode MethodNode { get; private set; }
            public CSharpExpressionIntermediateNode LayoutNode { get; private set; }
            public string LayoutType { get; private set; }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
            {
                NamespaceNode = node;
                base.VisitNamespaceDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                ClassNode = node;
                base.VisitClassDeclaration(node);
            }

            public override void VisitMethodDeclaration(MethodDeclarationIntermediateNode methodNode)
            {
                var topLevelExpressions = methodNode.Children.OfType<CSharpExpressionIntermediateNode>();
                foreach (var csharpExpression in topLevelExpressions)
                {
                    if (csharpExpression.Children.Count == 1)
                    {
                        var child = csharpExpression.Children[0];
                        if (child is IntermediateToken intermediateToken)
                        {
                            if (TryGetLayoutType(intermediateToken.Content, out string layoutType))
                            {
                                DidFindLayoutDeclaration = true;
                                MethodNode = methodNode;
                                LayoutNode = csharpExpression;
                                LayoutType = layoutType;
                            }
                        }
                    }
                }

                base.VisitMethodDeclaration(methodNode);
            }

            private bool TryGetLayoutType(string sourceCode, out string layoutType)
            {
                var match = LayoutSourceRegex.Match(sourceCode);
                if (match.Success)
                {
                    layoutType = match.Groups[1].Value;
                    return true;
                }
                else
                {
                    layoutType = null;
                    return false;
                }
            }
        }
    }
}
