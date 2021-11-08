// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentTemplateDiagnosticPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Runs after components/eventhandlers/ref/bind. We need to check for templates in all of those
    // places.
    public override int Order => 150;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var visitor = new Visitor();
        visitor.Visit(documentNode);

        for (var i = 0; i < visitor.Candidates.Count; i++)
        {
            var candidate = visitor.Candidates[i];
            candidate.Parent.Diagnostics.Add(ComponentDiagnosticFactory.Create_TemplateInvalidLocation(candidate.Node.Source));

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
                    ancestor is ComponentAttributeIntermediateNode ||

                    // Inside malformed ref attribute
                    ancestor is TagHelperPropertyIntermediateNode ||

                    // Inside a directive attribute
                    ancestor is TagHelperDirectiveAttributeIntermediateNode)
                {
                    Candidates.Add(new IntermediateNodeReference(Parent, node));
                }
            }
        }
    }
}
