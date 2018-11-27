// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class TemplateDiagnosticPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Runs after components/eventhandlers/ref/bind. We need to check for templates in all of those
        // places.
        public override int Order => 150;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var visitor = new Visitor();
            visitor.Visit(documentNode);

            for (var i = 0; i < visitor.Candidates.Count; i++)
            {
                var candidate = visitor.Candidates[i];
                candidate.Parent.Diagnostics.Add(BlazorDiagnosticFactory.Create_TemplateInvalidLocation(candidate.Node.Source));

                // Remove the offending node since we don't know how to render it. This means that the user won't get C#
                // completion at this location, which is fine because it's inside an HTML attribute.
                candidate.Remove();
            }
        }

        private class Visitor : IntermediateNodeWalker, IExtensionIntermediateNodeVisitor<TemplateIntermediateNode>
        {
            public List<IntermediateNodeReference> Candidates { get; } = new List<IntermediateNodeReference>();

            public void VisitExtension(TemplateIntermediateNode node)
            {
                // We found a template, let's check where it's located.
                for (var i = 0; i < Ancestors.Count; i++)
                {
                    var ancestor = Ancestors[i];

                    if (
                        // Inside markup attribute
                        ancestor is HtmlAttributeIntermediateNode ||

                        // Inside component attribute
                        ancestor is ComponentAttributeExtensionNode ||

                        // Inside malformed ref attribute
                        ancestor is TagHelperPropertyIntermediateNode)
                    {
                        Candidates.Add(new IntermediateNodeReference(Parent, node));
                    }
                }
            }
        }
    }
}
