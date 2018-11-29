// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Syntax
{
    internal static class GreenNodeExtensions
    {
        internal static InternalSyntax.SyntaxList<T> ToGreenList<T>(this SyntaxNode node) where T : GreenNode
        {
            return node != null ?
                ToGreenList<T>(node.Green) :
                default(InternalSyntax.SyntaxList<T>);
        }

        internal static InternalSyntax.SyntaxList<T> ToGreenList<T>(this GreenNode node) where T : GreenNode
        {
            return new InternalSyntax.SyntaxList<T>(node);
        }

        public static TNode WithAnnotationsGreen<TNode>(this TNode node, params SyntaxAnnotation[] annotations) where TNode : GreenNode
        {
            var newAnnotations = new List<SyntaxAnnotation>();
            foreach (var candidate in annotations)
            {
                if (!newAnnotations.Contains(candidate))
                {
                    newAnnotations.Add(candidate);
                }
            }

            if (newAnnotations.Count == 0)
            {
                var existingAnnotations = node.GetAnnotations();
                if (existingAnnotations == null || existingAnnotations.Length == 0)
                {
                    return node;
                }
                else
                {
                    return (TNode)node.SetAnnotations(null);
                }
            }
            else
            {
                return (TNode)node.SetAnnotations(newAnnotations.ToArray());
            }
        }

        public static TNode WithDiagnosticsGreen<TNode>(this TNode node, params RazorDiagnostic[] diagnostics) where TNode : GreenNode
        {
            return (TNode)node.SetDiagnostics(diagnostics);
        }
    }
}
