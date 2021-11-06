// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal class PreallocatedTagHelperAttributeOptimizationPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    // We want to run after the passes that 'lower' tag helpers. We also want this to run after DefaultTagHelperOptimizationPass.
    public override int Order => DefaultFeatureOrder + 1010;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        // There's no value in executing this pass at design time, it just prevents some allocations.
        if (documentNode.Options.DesignTime)
        {
            return;
        }

        var walker = new PreallocatedTagHelperWalker();
        walker.VisitDocument(documentNode);
    }

    internal class PreallocatedTagHelperWalker :
        IntermediateNodeWalker,
        IExtensionIntermediateNodeVisitor<DefaultTagHelperHtmlAttributeIntermediateNode>,
        IExtensionIntermediateNodeVisitor<DefaultTagHelperPropertyIntermediateNode>
    {
        private const string PreAllocatedAttributeVariablePrefix = "__tagHelperAttribute_";

        private ClassDeclarationIntermediateNode _classDeclaration;
        private int _variableCountOffset;
        private int _preallocatedDeclarationCount;

        public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
        {
            _classDeclaration = node;
            _variableCountOffset = node.Children.Count;

            VisitDefault(node);
        }

        public void VisitExtension(DefaultTagHelperHtmlAttributeIntermediateNode node)
        {
            if (node.Children.Count != 1 || !(node.Children.First() is HtmlContentIntermediateNode))
            {
                return;
            }

            var htmlContentNode = node.Children.First() as HtmlContentIntermediateNode;
            var plainTextValue = GetContent(htmlContentNode);

            PreallocatedTagHelperHtmlAttributeValueIntermediateNode declaration = null;

            for (var i = 0; i < _classDeclaration.Children.Count; i++)
            {
                var current = _classDeclaration.Children[i];

                if (current is PreallocatedTagHelperHtmlAttributeValueIntermediateNode existingDeclaration)
                {
                    if (string.Equals(existingDeclaration.AttributeName, node.AttributeName, StringComparison.Ordinal) &&
                        string.Equals(existingDeclaration.Value, plainTextValue, StringComparison.Ordinal) &&
                        existingDeclaration.AttributeStructure == node.AttributeStructure)
                    {
                        declaration = existingDeclaration;
                        break;
                    }
                }
            }

            if (declaration == null)
            {
                var variableCount = _classDeclaration.Children.Count - _variableCountOffset;
                var preAllocatedAttributeVariableName = PreAllocatedAttributeVariablePrefix + variableCount;
                declaration = new PreallocatedTagHelperHtmlAttributeValueIntermediateNode
                {
                    VariableName = preAllocatedAttributeVariableName,
                    AttributeName = node.AttributeName,
                    Value = plainTextValue,
                    AttributeStructure = node.AttributeStructure,
                };
                _classDeclaration.Children.Insert(_preallocatedDeclarationCount++, declaration);
            }

            var addPreAllocatedAttribute = new PreallocatedTagHelperHtmlAttributeIntermediateNode
            {
                VariableName = declaration.VariableName,
            };

            var nodeIndex = Parent.Children.IndexOf(node);
            Parent.Children[nodeIndex] = addPreAllocatedAttribute;
        }

        public void VisitExtension(DefaultTagHelperPropertyIntermediateNode node)
        {
            if (!(node.BoundAttribute.IsStringProperty || (node.IsIndexerNameMatch && node.BoundAttribute.IsIndexerStringProperty)) ||
                node.Children.Count != 1 ||
                !(node.Children.First() is HtmlContentIntermediateNode))
            {
                return;
            }

            var htmlContentNode = node.Children.First() as HtmlContentIntermediateNode;
            var plainTextValue = GetContent(htmlContentNode);

            PreallocatedTagHelperPropertyValueIntermediateNode declaration = null;

            for (var i = 0; i < _classDeclaration.Children.Count; i++)
            {
                var current = _classDeclaration.Children[i];

                if (current is PreallocatedTagHelperPropertyValueIntermediateNode existingDeclaration)
                {
                    if (string.Equals(existingDeclaration.AttributeName, node.AttributeName, StringComparison.Ordinal) &&
                        string.Equals(existingDeclaration.Value, plainTextValue, StringComparison.Ordinal) &&
                        existingDeclaration.AttributeStructure == node.AttributeStructure)
                    {
                        declaration = existingDeclaration;
                        break;
                    }
                }
            }

            if (declaration == null)
            {
                var variableCount = _classDeclaration.Children.Count - _variableCountOffset;
                var preAllocatedAttributeVariableName = PreAllocatedAttributeVariablePrefix + variableCount;
                declaration = new PreallocatedTagHelperPropertyValueIntermediateNode()
                {
                    VariableName = preAllocatedAttributeVariableName,
                    AttributeName = node.AttributeName,
                    Value = plainTextValue,
                    AttributeStructure = node.AttributeStructure,
                };
                _classDeclaration.Children.Insert(_preallocatedDeclarationCount++, declaration);
            }

            var setPreallocatedProperty = new PreallocatedTagHelperPropertyIntermediateNode(node)
            {
                VariableName = declaration.VariableName,
            };

            var nodeIndex = Parent.Children.IndexOf(node);
            Parent.Children[nodeIndex] = setPreallocatedProperty;
        }

        private string GetContent(HtmlContentIntermediateNode node)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsHtml)
                {
                    builder.Append(token.Content);
                }
            }

            return builder.ToString();
        }
    }
}
