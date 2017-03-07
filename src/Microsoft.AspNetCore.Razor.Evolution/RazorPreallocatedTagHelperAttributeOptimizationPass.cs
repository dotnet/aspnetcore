// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class RazorPreallocatedTagHelperAttributeOptimizationPass : RazorIRPassBase, IRazorIROptimizationPass
    {
        public override int Order => RazorIRPass.DefaultFeatureOrder;

        public override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var walker = new PreallocatedTagHelperWalker();
            walker.VisitDocument(irDocument);
        }

        internal class PreallocatedTagHelperWalker : RazorIRNodeWalker
        {
            private const string PreAllocatedAttributeVariablePrefix = "__tagHelperAttribute_";

            private ClassDeclarationIRNode _classDeclaration;
            private int _variableCountOffset;
            private int _preallocatedDeclarationCount = 0;

            public override void VisitClass(ClassDeclarationIRNode node)
            {
                _classDeclaration = node;
                _variableCountOffset = node.Children.Count;

                VisitDefault(node);
            }

            public override void VisitAddTagHelperHtmlAttribute(AddTagHelperHtmlAttributeIRNode node)
            {
                if (node.Children.Count != 1 || !(node.Children.First() is HtmlContentIRNode))
                {
                    return;
                }

                var plainTextValue = (node.Children.First() as HtmlContentIRNode).Content;
                DeclarePreallocatedTagHelperHtmlAttributeIRNode declaration = null;

                for (var i = 0; i < _classDeclaration.Children.Count; i++)
                {
                    var current = _classDeclaration.Children[i];

                    if (current is DeclarePreallocatedTagHelperHtmlAttributeIRNode)
                    {
                        var existingDeclaration = (DeclarePreallocatedTagHelperHtmlAttributeIRNode)current;

                        if (string.Equals(existingDeclaration.Name, node.Name, StringComparison.Ordinal) &&
                            string.Equals(existingDeclaration.Value, plainTextValue, StringComparison.Ordinal) &&
                            existingDeclaration.ValueStyle == node.ValueStyle)
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
                    declaration = new DeclarePreallocatedTagHelperHtmlAttributeIRNode
                    {
                        VariableName = preAllocatedAttributeVariableName,
                        Name = node.Name,
                        Value = plainTextValue,
                        ValueStyle = node.ValueStyle,
                        Parent = _classDeclaration
                    };
                    _classDeclaration.Children.Insert(_preallocatedDeclarationCount++, declaration);
                }

                var addPreAllocatedAttribute = new AddPreallocatedTagHelperHtmlAttributeIRNode
                {
                    VariableName = declaration.VariableName,
                    Parent = node.Parent
                };

                var nodeIndex = node.Parent.Children.IndexOf(node);
                node.Parent.Children[nodeIndex] = addPreAllocatedAttribute;
            }

            public override void VisitSetTagHelperProperty(SetTagHelperPropertyIRNode node)
            {
                if (!(node.Descriptor.IsStringProperty || (node.IsIndexerNameMatch && node.Descriptor.IsIndexerStringProperty)) ||
                    node.Children.Count != 1 ||
                    !(node.Children.First() is HtmlContentIRNode))
                {
                    return;
                }

                var plainTextValue = (node.Children.First() as HtmlContentIRNode).Content;

                DeclarePreallocatedTagHelperAttributeIRNode declaration = null;

                for (var i = 0; i < _classDeclaration.Children.Count; i++)
                {
                    var current = _classDeclaration.Children[i];

                    if (current is DeclarePreallocatedTagHelperAttributeIRNode)
                    {
                        var existingDeclaration = (DeclarePreallocatedTagHelperAttributeIRNode)current;

                        if (string.Equals(existingDeclaration.Name, node.AttributeName, StringComparison.Ordinal) &&
                            string.Equals(existingDeclaration.Value, plainTextValue, StringComparison.Ordinal) &&
                            existingDeclaration.ValueStyle == node.ValueStyle)
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
                    declaration = new DeclarePreallocatedTagHelperAttributeIRNode
                    {
                        VariableName = preAllocatedAttributeVariableName,
                        Name = node.AttributeName,
                        Value = plainTextValue,
                        ValueStyle = node.ValueStyle,
                        Parent = _classDeclaration
                    };
                    _classDeclaration.Children.Insert(_preallocatedDeclarationCount++, declaration);
                }

                var setPreallocatedProperty = new SetPreallocatedTagHelperPropertyIRNode
                {
                    VariableName = declaration.VariableName,
                    AttributeName = node.AttributeName,
                    TagHelperTypeName = node.TagHelperTypeName,
                    PropertyName = node.PropertyName,
                    Descriptor = node.Descriptor,
                    Binding = node.Binding,
                    Parent = node.Parent,
                    IsIndexerNameMatch = node.IsIndexerNameMatch,
                };

                var nodeIndex = node.Parent.Children.IndexOf(node);
                node.Parent.Children[nodeIndex] = setPreallocatedProperty;
            }
        }
    }
}
