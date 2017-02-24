using System;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace RazorPageGenerator
{
    class RemovePragmaChecksumFeature : RazorIRPassBase, IRazorIROptimizationPass
    {
        public override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var walker = new Walker();
            walker.Visit(irDocument);

            walker.ChecksumNode.Parent.Children.Remove(walker.ChecksumNode);
        }

        private class Walker : RazorIRNodeWalker
        {
            public ChecksumIRNode ChecksumNode { get; private set; }

            public override void VisitChecksum(ChecksumIRNode node)
            {
                ChecksumNode = node;
            }
        }
    }
}