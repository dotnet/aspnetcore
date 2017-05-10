// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

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

        public static RazorMethodDeclarationIRNode FindPrimaryMethod(this DocumentIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return FindWithAnnotation<RazorMethodDeclarationIRNode>(node, CommonAnnotations.PrimaryMethod);
        }

        public static NamespaceDeclarationIRNode FindPrimaryNamespace(this DocumentIRNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            return FindWithAnnotation<NamespaceDeclarationIRNode>(node, CommonAnnotations.PrimaryNamespace);
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
    }
}
