// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    internal static class DocumentIntermediateNodeExtensions
    {
        public static IReadOnlyList<IntermediateNodeReference> FindDescendantReferences<TNode>(this DocumentIntermediateNode document)
            where TNode : IntermediateNode
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var visitor = new ReferenceVisitor<TNode>();
            visitor.Visit(document);
            return visitor.References;
        }

        private class ReferenceVisitor<TNode> : IntermediateNodeWalker
            where TNode : IntermediateNode
        {
            public List<IntermediateNodeReference> References = new List<IntermediateNodeReference>();

            public override void VisitDefault(IntermediateNode node)
            {
                base.VisitDefault(node);

                // Use a post-order traversal because references are used to replace nodes, and thus
                // change the parent nodes.
                //
                // This ensures that we always operate on the leaf nodes first.
                if (node is TNode)
                {
                    References.Add(new IntermediateNodeReference(Parent, node));
                }
            }
        }
    }
}
