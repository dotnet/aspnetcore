// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentChildContentDiagnosticPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Runs after components/eventhandlers/ref/bind/templates. We want to validate every component
    // and it's usage of ChildContent.
    public override int Order => 160;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var visitor = new Visitor();
        visitor.Visit(documentNode);
    }

    private class Visitor : IntermediateNodeWalker
    {
        public override void VisitComponent(ComponentIntermediateNode node)
        {
            // Check for properties that are set by both element contents (body) and the attribute itself.
            foreach (var childContent in node.ChildContents)
            {
                foreach (var attribute in node.Attributes)
                {
                    if (attribute.AttributeName == childContent.AttributeName)
                    {
                        node.Diagnostics.Add(ComponentDiagnosticFactory.Create_ChildContentSetByAttributeAndBody(
                            attribute.Source,
                            attribute.AttributeName));
                    }
                }
            }

            base.VisitDefault(node);
        }

        public override void VisitComponentChildContent(ComponentChildContentIntermediateNode node)
        {
            // Check that each child content has a unique parameter name within its scope. This is important
            // because the parameter name can be implicit, and it doesn't work well when nested.
            if (node.IsParameterized)
            {
                for (var i = 0; i < Ancestors.Count - 1; i++)
                {
                    var ancestor = Ancestors[i] as ComponentChildContentIntermediateNode;
                    if (ancestor != null &&
                        ancestor.IsParameterized &&
                        string.Equals(node.ParameterName, ancestor.ParameterName, StringComparison.Ordinal))
                    {
                        // Duplicate name. We report an error because this will almost certainly also lead to an error
                        // from the C# compiler that's way less clear.
                        node.Diagnostics.Add(ComponentDiagnosticFactory.Create_ChildContentRepeatedParameterName(
                            node.Source,
                            node,
                            (ComponentIntermediateNode)Ancestors[0], // Enclosing component
                            ancestor, // conflicting child content node
                            (ComponentIntermediateNode)Ancestors[i + 1]));  // Enclosing component of conflicting child content node
                    }
                }
            }

            base.VisitDefault(node);
        }
    }
}
