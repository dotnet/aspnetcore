// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    // This pass:
    // 1. Adds diagnostics for missing generic type arguments
    // 2. Rewrites the type name of the component to substitute generic type arguments
    // 3. Rewrites the type names of parameters/child content to substitute generic type arguments
    internal class GenericComponentPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Runs after components/eventhandlers/ref/bind/templates. We want to validate every component
        // and it's usage of ChildContent.
        public override int Order => 160;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var visitor = new Visitor();
            visitor.Visit(documentNode);
        }

        
        private class Visitor : IntermediateNodeWalker, IExtensionIntermediateNodeVisitor<ComponentExtensionNode>
        {
            public void VisitExtension(ComponentExtensionNode node)
            {
                if (node.Component.IsGenericTypedComponent())
                {
                    // Not generic, ignore.
                    Process(node);
                }

                base.VisitDefault(node);
            }

            private void Process(ComponentExtensionNode node)
            {
                // First collect all of the information we have about each type parameter
                var bindings = new Dictionary<string, GenericTypeNameRewriter.Binding>();
                foreach (var attribute in node.Component.GetTypeParameters())
                {
                    bindings.Add(attribute.Name, new GenericTypeNameRewriter.Binding() { Attribute = attribute, });
                }

                foreach (var typeArgumentNode in node.TypeArguments)
                {
                    var binding = bindings[typeArgumentNode.TypeParameterName];
                    binding.Node = typeArgumentNode;
                    binding.Content = GetContent(typeArgumentNode);
                }

                // Right now we don't have type inference, so all type arguments are required.
                var missing = new List<BoundAttributeDescriptor>();
                foreach (var binding in bindings)
                {
                    if (binding.Value.Node == null || string.IsNullOrWhiteSpace(binding.Value.Content))
                    {
                        missing.Add(binding.Value.Attribute);
                    }
                }

                if (missing.Count > 0)
                {
                    // We add our own error for this because its likely the user will see other errors due
                    // to incorrect codegen without the types. Our errors message will pretty clearly indicate
                    // what to do, whereas the other errors might be confusing.
                    node.Diagnostics.Add(BlazorDiagnosticFactory.Create_GenericComponentMissingTypeArgument(node.Source, node, missing));
                }

                var rewriter = new GenericTypeNameRewriter(bindings);

                // Rewrite the component type name
                node.TypeName = RewriteTypeName(rewriter, node.TypeName);

                foreach (var attribute in node.Attributes)
                {
                    if (attribute.BoundAttribute?.IsGenericTypedProperty() ?? false && attribute.TypeName != null)
                    {
                        // If we know the type name, then replace any generic type parameter inside it with
                        // the known types.
                        attribute.TypeName = RewriteTypeName(rewriter, attribute.TypeName);
                    }
                }

                foreach (var childContent in node.ChildContents)
                {
                    if (childContent.BoundAttribute?.IsGenericTypedProperty() ?? false && childContent.TypeName != null)
                    {
                        // If we know the type name, then replace any generic type parameter inside it with
                        // the known types.
                        childContent.TypeName = RewriteTypeName(rewriter, childContent.TypeName);
                    }
                }
            }

            private string RewriteTypeName(GenericTypeNameRewriter rewriter, string typeName)
            {
                var parsed = SyntaxFactory.ParseTypeName(typeName);
                var rewritten = (TypeSyntax)rewriter.Visit(parsed);
                return rewritten.ToFullString();
            }

            private string GetContent(ComponentTypeArgumentExtensionNode node)
            {
                return string.Join(string.Empty, node.FindDescendantNodes<IntermediateToken>().Where(t => t.IsCSharp).Select(t => t.Content));
            }
        }
    }
}
