// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal class ComponentReferenceCaptureLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run after component lowering pass
        public override int Order => 50;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            if (!IsComponentDocument(documentNode))
            {
                return;
            }

            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                // Nothing to do, bail. We can't function without the standard structure.
                return;
            }

            var references = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeIntermediateNode>();
            for (var i = 0; i < references.Count; i++)
            {
                var reference = references[i];
                var node = (TagHelperDirectiveAttributeIntermediateNode)reference.Node;

                if (node.TagHelper.IsRefTagHelper())
                {
                    // We've found an @ref directive attribute.
                    //
                    // If we can't get a nonempty identifier, do nothing because there will
                    // already be a diagnostic for empty values
                    var identifier = DetermineIdentifierToken(node);
                    if (identifier == null)
                    {
                        continue;
                    }

                    var rewritten = RewriteUsage(reference.Parent, identifier);
                    reference.Replace(rewritten);

                    // Now we need to check if the field generation has been suppressed.
                    //
                    // You have to suppress field generation for generic types because we don't know the
                    // type name to create the field.
                    var generateField = ShouldGenerateField(reference.Parent);

                    // Insert the field with other fields, near the top of the class.
                    if (generateField)
                    {
                        var position = 0;
                        while (position < @class.Children.Count && @class.Children[i] is FieldDeclarationIntermediateNode)
                        {
                            position++;
                        }

                        @class.Children.Insert(position, CreateField(rewritten.FieldTypeName, identifier));
                    }
                }
            }
        }

        private ReferenceCaptureIntermediateNode RewriteUsage(IntermediateNode parent, IntermediateToken identifier)
        {
            // Determine whether this is an element capture or a component capture, and
            // if applicable the type name that will appear in the resulting capture code
            var componentTagHelper = (parent as ComponentIntermediateNode)?.Component;
            if (componentTagHelper != null)
            {
                return new ReferenceCaptureIntermediateNode(identifier, componentTagHelper.GetTypeName());
            }
            else
            {
                return new ReferenceCaptureIntermediateNode(identifier);
            }
        }

        private bool ShouldGenerateField(IntermediateNode parent)
        {
            var parameters = parent.FindDescendantNodes<TagHelperDirectiveAttributeParameterIntermediateNode>();
            for (var i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                if (parameter.TagHelper.IsRefTagHelper() && parameter.BoundAttributeParameter.Name == "suppressField")
                {
                    if (parameter.HasDiagnostics)
                    {
                        parent.Diagnostics.AddRange(parameter.GetAllDiagnostics());
                    }

                    parent.Children.Remove(parameter);

                    if (parameter.AttributeStructure == AttributeStructure.Minimized)
                    {
                        return false;
                    }

                    // We do not support non-minimized attributes here because we can't allow the value to be dynamic.
                    // As a design/experience decision, we don't let you write @ref:suppressField="false" even though
                    // we could parse it. The rationale is that it's misleading, you type something that looks like code,
                    // but it's not really.
                    parent.Diagnostics.Add(ComponentDiagnosticFactory.Create_RefSuppressFieldNotMinimized(parameter.Source));
                }
            }

            if (parent is ComponentIntermediateNode component && component.Component.IsGenericTypedComponent())
            {
                // We cannot automatically generate a 'ref' field for generic components because we don't know
                // how to write the type.
                parent.Diagnostics.Add(ComponentDiagnosticFactory.Create_RefSuppressFieldRequiredForGeneric(parent.Source));
                return false;
            }

            return true;
        }

        private IntermediateNode CreateField(string fieldType, IntermediateToken identifier)
        {
            return new FieldDeclarationIntermediateNode()
            {
                FieldName = identifier.Content,
                FieldType = fieldType,
                Modifiers = { "private" },
                SuppressWarnings =
                {
                    "0414", // Field is assigned by never used
                    "0169", // Field is never used
                }, 
            };
        }

        private IntermediateToken DetermineIdentifierToken(TagHelperDirectiveAttributeIntermediateNode attributeNode)
        {
            IntermediateToken foundToken = null;

            if (attributeNode.Children.Count == 1)
            {
                if (attributeNode.Children[0] is IntermediateToken token)
                {
                    foundToken = token;
                }
                else if (attributeNode.Children[0] is CSharpExpressionIntermediateNode csharpNode)
                {
                    if (csharpNode.Children.Count == 1)
                    {
                        foundToken = csharpNode.Children[0] as IntermediateToken;
                    }
                }
            }
            
            return !string.IsNullOrWhiteSpace(foundToken?.Content) ? foundToken : null;
        }
    }
}
