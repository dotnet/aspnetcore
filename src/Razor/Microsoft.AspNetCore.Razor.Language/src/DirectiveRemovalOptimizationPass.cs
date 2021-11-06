// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DirectiveRemovalOptimizationPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    public override int Order => DefaultFeatureOrder + 50;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        var visitor = new Visitor();
        visitor.VisitDocument(documentNode);

        foreach (var nodeReference in visitor.DirectiveNodes)
        {
            // Lift the diagnostics in the directive node up to the document node.
            for (var i = 0; i < nodeReference.Node.Diagnostics.Count; i++)
            {
                documentNode.Diagnostics.Add(nodeReference.Node.Diagnostics[i]);
            }

            nodeReference.Remove();
        }
    }

    private class Visitor : IntermediateNodeWalker
    {
        public IList<IntermediateNodeReference> DirectiveNodes { get; } = new List<IntermediateNodeReference>();

        public override void VisitDirective(DirectiveIntermediateNode node)
        {
            DirectiveNodes.Add(new IntermediateNodeReference(Parent, node));
        }
    }
}
