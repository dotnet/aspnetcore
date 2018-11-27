// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public static class IntermediateNodeExtensions
    {
        private static readonly IReadOnlyList<RazorDiagnostic> EmptyDiagnostics = Array.Empty<RazorDiagnostic>();

        public static bool IsImported(this IntermediateNode node)
        {
            return ReferenceEquals(node.Annotations[CommonAnnotations.Imported], CommonAnnotations.Imported);
        }

        public static IReadOnlyList<RazorDiagnostic> GetAllDiagnostics(this IntermediateNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            HashSet<RazorDiagnostic> diagnostics = null;

            AddAllDiagnostics(node);

            return diagnostics?.ToList() ?? EmptyDiagnostics;

            void AddAllDiagnostics(IntermediateNode n)
            {
                if (n.HasDiagnostics)
                {
                    if (diagnostics == null)
                    {
                        diagnostics = new HashSet<RazorDiagnostic>();
                    }

                    diagnostics.UnionWith(n.Diagnostics);
                }

                for (var i = 0; i < n.Children.Count; i++)
                {
                    AddAllDiagnostics(n.Children[i]);
                }
            }
        }

        public static IReadOnlyList<TNode> FindDescendantNodes<TNode>(this IntermediateNode node)
            where TNode : IntermediateNode
        {
            var visitor = new Visitor<TNode>();
            visitor.Visit(node);

            if (visitor.Results.Count > 0 && visitor.Results[0] == node)
            {
                // Don't put the node itself in the results
                visitor.Results.Remove((TNode)node);
            }

            return visitor.Results;
        }

        private class Visitor<TNode> : IntermediateNodeWalker where TNode : IntermediateNode
        {
            public List<TNode> Results { get; } = new List<TNode>();

            public override void VisitDefault(IntermediateNode node)
            {
                if (node is TNode match)
                {
                    Results.Add(match);
                }

                base.VisitDefault(node);
            }
        }
    }
}
