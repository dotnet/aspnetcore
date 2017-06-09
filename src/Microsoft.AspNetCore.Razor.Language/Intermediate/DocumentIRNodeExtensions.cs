// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public static class DocumentIRNodeExtensions
    {
        public static ClassDeclarationIRNode FindPrimaryClass(this DocumentIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return FindWithAnnotation<ClassDeclarationIRNode>(node, CommonAnnotations.PrimaryClass);
        }

        public static MethodDeclarationIRNode FindPrimaryMethod(this DocumentIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return FindWithAnnotation<MethodDeclarationIRNode>(node, CommonAnnotations.PrimaryMethod);
        }

        public static NamespaceDeclarationIRNode FindPrimaryNamespace(this DocumentIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return FindWithAnnotation<NamespaceDeclarationIRNode>(node, CommonAnnotations.PrimaryNamespace);
        }

        public static IReadOnlyList<RazorIRNodeReference> FindDirectiveReferences(this DocumentIRNode node, DirectiveDescriptor directive)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            var visitor = new DirectiveVisitor(directive);
            visitor.Visit(node);
            return visitor.Directives;
        }

        private static T FindWithAnnotation<T>(RazorIRNode node, object annotation) where T : RazorIRNode
        {
            if (node is T target && object.ReferenceEquals(target.Annotations[annotation], annotation))
            {
                return target;
            }

            for (var i = 0; i < node.Children.Count; i++)
            {
                var result = FindWithAnnotation<T>(node.Children[i], annotation);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private class DirectiveVisitor : RazorIRNodeWalker
        {
            private readonly DirectiveDescriptor _directive;

            public DirectiveVisitor(DirectiveDescriptor directive)
            {
                _directive = directive;
            }

            public List<RazorIRNodeReference> Directives = new List<RazorIRNodeReference>();

            public override void VisitDirective(DirectiveIRNode node)
            {
                if (_directive == node.Descriptor)
                {
                    Directives.Add(new RazorIRNodeReference(Parent, node));
                }

                base.VisitDirective(node);
            }
        }
    }
}
