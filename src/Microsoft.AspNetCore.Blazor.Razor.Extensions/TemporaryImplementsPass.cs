// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// This code is temporary. It finds top-level expressions of the form
    ///     @Implements<SomeInterfaceType>()
    /// ... and converts them into interface declarations on the class.
    /// Once we're able to add Blazor-specific directives and have them show up in tooling,
    /// we'll replace this with a simpler and cleaner "@implements SomeInterfaceType" directive.
    /// </summary>
    internal class TemporaryImplementsPass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
    {
        // Example: "Implements<MyApp.Namespace.ISomeType<T1, T2>>()"
        // Captures: MyApp.Namespace.ISomeType<T1, T2>
        private static readonly Regex ImplementsSourceRegex
            = new Regex(@"^\s*Implements\s*<(.+)\>\s*\(\s*\)\s*$");

        public static void Register(IRazorEngineBuilder configuration)
        {
            configuration.Features.Add(new TemporaryImplementsPass());
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var visitor = new Visitor();
            visitor.Visit(documentNode);

            foreach (var implementsNode in visitor.ImplementsNodes)
            {
                visitor.MethodNode.Children.Remove(implementsNode);
            }
            
            if (visitor.ClassNode.Interfaces == null)
            {
                visitor.ClassNode.Interfaces = new List<string>();
            }

            foreach (var implementsType in visitor.ImplementsTypes)
            {
                visitor.ClassNode.Interfaces.Add(implementsType);
            }
        }

        private class Visitor : IntermediateNodeWalker
        {
            public ClassDeclarationIntermediateNode ClassNode { get; private set; }
            public MethodDeclarationIntermediateNode MethodNode { get; private set; }
            public List<CSharpExpressionIntermediateNode> ImplementsNodes { get; private set; }
                = new List<CSharpExpressionIntermediateNode>();
            public List<string> ImplementsTypes { get; private set; }
                = new List<string>();

            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                ClassNode = node;
                base.VisitClassDeclaration(node);
            }

            public override void VisitMethodDeclaration(MethodDeclarationIntermediateNode methodNode)
            {
                MethodNode = methodNode;

                var topLevelExpressions = methodNode.Children.OfType<CSharpExpressionIntermediateNode>();
                foreach (var csharpExpression in topLevelExpressions)
                {
                    if (csharpExpression.Children.Count == 1)
                    {
                        var child = csharpExpression.Children[0];
                        if (child is IntermediateToken intermediateToken)
                        {
                            if (TryGetImplementsType(intermediateToken.Content, out string implementsType))
                            {
                                ImplementsNodes.Add(csharpExpression);
                                ImplementsTypes.Add(implementsType);
                            }
                        }
                    }
                }

                base.VisitMethodDeclaration(methodNode);
            }

            private bool TryGetImplementsType(string sourceCode, out string implementsType)
            {
                var match = ImplementsSourceRegex.Match(sourceCode);
                if (match.Success)
                {
                    implementsType = match.Groups[1].Value;
                    return true;
                }
                else
                {
                    implementsType = null;
                    return false;
                }
            }
        }
    }
}