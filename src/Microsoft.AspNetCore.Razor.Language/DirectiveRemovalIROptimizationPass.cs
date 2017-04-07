// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DirectiveRemovalIROptimizationPass : RazorIRPassBase, IRazorIROptimizationPass
    {
        public override int Order => RazorIRPass.DefaultFeatureOrder + 50;

        public override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var visitor = new Visitor();
            visitor.VisitDocument(irDocument);

            foreach (var node in visitor.DirectiveNodes)
            {
                node.Parent.Children.Remove(node);
            }
        }

        private class Visitor : RazorIRNodeWalker
        {
            public IList<DirectiveIRNode> DirectiveNodes { get; } = new List<DirectiveIRNode>();

            public override void VisitDirective(DirectiveIRNode node)
            {
                DirectiveNodes.Add(node);
            }
        }
    }
}
