// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace RazorPageGenerator
{
    public class RemovePragmaChecksumFeature : RazorIRPassBase, IRazorIROptimizationPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var walker = new Walker();
            walker.Visit(irDocument);

            walker.Checksum.parent.Children.Remove(walker.Checksum.node);
        }

        private class Walker : RazorIRNodeWalker
        {
            public (ChecksumIRNode node, RazorIRNode parent) Checksum { get; private set; }

            public override void VisitChecksum(ChecksumIRNode node)
            {
                Checksum = (node, Parent);
            }
        }
    }
}