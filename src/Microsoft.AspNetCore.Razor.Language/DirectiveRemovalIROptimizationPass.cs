// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DirectiveRemovalIROptimizationPass : RazorIRPassBase, IRazorIROptimizationPass
    {
        public override int Order => DefaultFeatureOrder + 50;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var visitor = new Visitor();
            visitor.VisitDocument(irDocument);

            foreach (var (node, parent) in visitor.DirectiveNodes)
            {
                parent.Children.Remove(node);
            }
        }

        private class Visitor : RazorIRNodeWalker
        {
            public IList<(DirectiveIRNode node, RazorIRNode parent)> DirectiveNodes { get; } = new List<(DirectiveIRNode node, RazorIRNode parent)>();

            public override void VisitDirective(DirectiveIRNode node)
            {
                DirectiveNodes.Add((node, Parent));
            }
        }
    }
}
