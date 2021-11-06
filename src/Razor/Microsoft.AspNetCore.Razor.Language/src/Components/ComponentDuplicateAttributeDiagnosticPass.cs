// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentMarkupDiagnosticPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    public const int DefaultOrder = 10000;

    public override int Order => DefaultOrder;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        var visitor = new Visitor();
        visitor.Visit(documentNode);
    }

    private class Visitor : IntermediateNodeWalker
    {
        private readonly Dictionary<string, (string name, IntermediateNode node)> _attributes = new Dictionary<string, (string, IntermediateNode)>(StringComparer.OrdinalIgnoreCase);

        public override void VisitMarkupElement(MarkupElementIntermediateNode node)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is HtmlAttributeIntermediateNode attribute && attribute.AttributeName != null)
                {
                    if (_attributes.TryGetValue(attribute.AttributeName, out var other))
                    {
                        // As a special case we want to point it out explicitly where a directive or other construct
                        // has emitted an attribute that causes a conflict. We're already looking at the lowered version
                        // of this construct, so it's easy to detect. We just need the original name to report the issue.
                        //
                        // Example: `bind-value` will set `value` and `onchange`.

                        var originalAttributeName =
                            attribute.Annotations[ComponentMetadata.Common.OriginalAttributeName] as string ??
                            other.node.Annotations[ComponentMetadata.Common.OriginalAttributeName] as string;
                        if (originalAttributeName != null)
                        {
                            other.node.Diagnostics.Add(ComponentDiagnosticFactory.Create_DuplicateMarkupAttributeDirective(
                                other.name,
                                originalAttributeName,
                                other.node.Source ?? node.Source));
                        }
                        else
                        {
                            // This is a conflict in the code the user wrote.
                            other.node.Diagnostics.Add(ComponentDiagnosticFactory.Create_DuplicateMarkupAttribute(
                                other.name,
                                other.node.Source ?? node.Source));
                        }
                    }

                    // Replace the attribute we were previously tracking. Then if you have three, the two on the left will have
                    // diagnostics.
                    _attributes[attribute.AttributeName] = (attribute.AttributeName, attribute);
                }
            }

            _attributes.Clear();
            base.VisitMarkupElement(node);
        }

        public override void VisitComponent(ComponentIntermediateNode node)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                // Note that we don't handle ChildContent cases here. Those have their own pass for diagnostics.
                if (node.Children[i] is ComponentAttributeIntermediateNode attribute && attribute.AttributeName != null)
                {
                    if (_attributes.TryGetValue(attribute.AttributeName, out var other))
                    {
                        // As a special case we want to point it out explicitly where a directive or other construct
                        // has emitted an attribute that causes a conflict. We're already looking at the lowered version
                        // of this construct, so it's easy to detect. We just need the original name to report the issue.
                        //
                        // Example: `bind-Value` will set `Value` and `ValueChanged`.
                        var originalAttributeName =
                            attribute.Annotations[ComponentMetadata.Common.OriginalAttributeName] as string ??
                            other.node.Annotations[ComponentMetadata.Common.OriginalAttributeName] as string;
                        if (originalAttributeName != null)
                        {
                            other.node.Diagnostics.Add(ComponentDiagnosticFactory.Create_DuplicateComponentParameterDirective(
                                other.name,
                                originalAttributeName,
                                other.node.Source ?? node.Source));
                        }
                        else
                        {
                            // This is a conflict in the code the user wrote.
                            other.node.Diagnostics.Add(ComponentDiagnosticFactory.Create_DuplicateComponentParameter(
                                other.name,
                                other.node.Source ?? node.Source));
                        }
                    }

                    // Replace the attribute we were previously tracking. Then if you have three, the two on the left will have
                    // diagnostics.
                    _attributes[attribute.AttributeName] = (attribute.AttributeName, attribute);
                }
            }

            _attributes.Clear();
            base.VisitComponent(node);
        }
    }
}
