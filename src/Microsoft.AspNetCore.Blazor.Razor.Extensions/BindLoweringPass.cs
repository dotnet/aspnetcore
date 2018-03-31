// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class BindLoweringPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                // Nothing to do, bail. We can't function without the standard structure.
                return;
            }

            // For each bind *usage* we need to rewrite the tag helper node to map to basic constructs.
            var nodes = documentNode.FindDescendantNodes<TagHelperIntermediateNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                ProcessDuplicates(node);

                for (var j = node.Children.Count - 1; j >= 0; j--)
                {
                    var attributeNode = node.Children[j] as ComponentAttributeExtensionNode;
                    if (attributeNode != null &&
                        attributeNode.TagHelper != null &&
                        attributeNode.TagHelper.IsBindTagHelper())
                    {
                        RewriteUsage(node, j, attributeNode);
                    }
                }
            }
        }

        private void ProcessDuplicates(TagHelperIntermediateNode node)
        {
            // Reverse order because we will remove nodes.
            //
            // Each 'property' node could be duplicated if there are multiple tag helpers that match that
            // particular attribute. This is common in our approach, which relies on 'fallback' tag helpers
            // that overlap with more specific ones.
            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                // For each usage of the general 'fallback' bind tag helper, it could duplicate
                // the usage of a more specific one. Look for duplicates and remove the fallback.
                var attributeNode = node.Children[i] as ComponentAttributeExtensionNode;
                if (attributeNode != null &&
                    attributeNode.TagHelper != null &&
                    attributeNode.TagHelper.IsFallbackBindTagHelper())
                {
                    for (var j = 0; j < node.Children.Count; j++)
                    {
                        var duplicate = node.Children[j] as ComponentAttributeExtensionNode;
                        if (duplicate != null &&
                            duplicate.TagHelper != null &&
                            duplicate.TagHelper.IsBindTagHelper() &&
                            duplicate.AttributeName == attributeNode.AttributeName &&
                            !object.ReferenceEquals(attributeNode, duplicate))
                        {
                            // Found a duplicate - remove the 'fallback' in favor of the
                            // more specific tag helper.
                            node.Children.RemoveAt(i);
                            node.TagHelpers.Remove(attributeNode.TagHelper);
                            break;
                        }
                    }
                }

                // Also treat the general <input bind="..." /> as a 'fallback' for that case and remove it.
                // This is a workaround for a limitation where you can't write a tag helper that binds only
                // when a specific attribute is **not** present.
                if (attributeNode != null &&
                    attributeNode.TagHelper != null &&
                    attributeNode.TagHelper.IsInputElementFallbackBindTagHelper())
                {
                    for (var j = 0; j < node.Children.Count; j++)
                    {
                        var duplicate = node.Children[j] as ComponentAttributeExtensionNode;
                        if (duplicate != null &&
                            duplicate.TagHelper != null &&
                            duplicate.TagHelper.IsInputElementBindTagHelper() &&
                            duplicate.AttributeName == attributeNode.AttributeName &&
                            !object.ReferenceEquals(attributeNode, duplicate))
                        {
                            // Found a duplicate - remove the 'fallback' input tag helper in favor of the
                            // more specific tag helper.
                            node.Children.RemoveAt(i);
                            node.TagHelpers.Remove(attributeNode.TagHelper);
                            break;
                        }
                    }
                }
            }

            // If we still have duplicates at this point then they are genuine conflicts.
            var duplicates = node.Children
                .OfType<ComponentAttributeExtensionNode>()
                .GroupBy(p => p.AttributeName)
                .Where(g => g.Count() > 1);

            foreach (var duplicate in duplicates)
            {
                node.Diagnostics.Add(BlazorDiagnosticFactory.CreateBindAttribute_Duplicates(
                    node.Source,
                    duplicate.Key,
                    duplicate.ToArray()));
                foreach (var property in duplicate)
                {
                    node.Children.Remove(property);
                }
            }
        }

        private void RewriteUsage(TagHelperIntermediateNode node, int index, ComponentAttributeExtensionNode attributeNode)
        {
            // Bind works similarly to a macro, it always expands to code that the user could have written.
            //
            // For the nodes that are related to the bind-attribute rewrite them to look like a pair of
            // 'normal' HTML attributes similar to the following transformation.
            //
            // Input:   <MyComponent bind-Value="@currentCount" />
            // Output:  <MyComponent Value ="...<get the value>..." ValueChanged ="... <set the value>..." />
            //
            // This means that the expression that appears inside of 'bind' must be an LValue or else
            // there will be errors. In general the errors that come from C# in this case are good enough
            // to understand the problem.
            //
            // The BindMethods calls are required in this case because to give us a good experience. They
            // use overloading to ensure that can get an Action<object> that will convert and set an arbitrary
            // value.
            //
            // We also assume that the element will be treated as a component for now because
            // multiple passes handle 'special' tag helpers. We have another pass that translates
            // a tag helper node back into 'regular' element when it doesn't have an associated component
            if (!TryComputeAttributeNames(
                node,
                attributeNode.AttributeName,
                out var valueAttributeName,
                out var changeAttributeName,
                out var valueAttribute,
                out var changeAttribute))
            {
                // Skip anything we can't understand. It's important that we don't crash, that will bring down
                // the build.
                return;
            }

            var originalContent = GetAttributeContent(attributeNode);
            if (string.IsNullOrEmpty(originalContent))
            {
                // This can happen in error cases, the parser will already have flagged this
                // as an error, so ignore it.
                return;
            }

            // Look for a matching format node. If we find one then we need to pass the format into the
            // two nodes we generate.
            string format = null;
            if (TryGetFormatNode(node,
                attributeNode,
                valueAttributeName,
                out var formatNode))
            {
                // Don't write the format out as its own attribute;
                node.Children.Remove(formatNode);
                format = GetAttributeContent(formatNode);
            }

            var valueAttributeNode = new ComponentAttributeExtensionNode(attributeNode)
            {
                AttributeName = valueAttributeName,
                BoundAttribute = valueAttribute, // Might be null if it doesn't match a component attribute
                PropertyName = valueAttribute?.GetPropertyName(),
                TagHelper = valueAttribute == null ? null : attributeNode.TagHelper,
            };
            node.Children.Insert(index, valueAttributeNode);

            // Now rewrite the content of the value node to look like:
            //
            // BindMethods.GetValue(<code>) OR
            // BindMethods.GetValue(<code>, <format>)
            //
            // For now, the way this is done isn't debuggable. But since the expression
            // passed here must be an LValue, it's probably not important.
            var valueNodeContent = format == null ?
                $"{BlazorApi.BindMethods.GetValue}({originalContent})" :
                $"{BlazorApi.BindMethods.GetValue}({originalContent}, {format})";
            valueAttributeNode.Children.Clear();
            valueAttributeNode.Children.Add(new CSharpExpressionIntermediateNode()
            {
                Children =
                {
                    new IntermediateToken()
                    {
                        Content = valueNodeContent,
                        Kind = TokenKind.CSharp
                    },
                },
            });

            var changeAttributeNode = new ComponentAttributeExtensionNode(attributeNode)
            {
                AttributeName = changeAttributeName,
                BoundAttribute = changeAttribute, // Might be null if it doesn't match a component attribute
                PropertyName = changeAttribute?.GetPropertyName(),
                TagHelper = changeAttribute == null ? null : attributeNode.TagHelper,
            };
            node.Children[index + 1] = changeAttributeNode;

            // Now rewrite the content of the change-handler node. There are two cases we care about
            // here. If it's a component attribute, then don't use the 'BindMethods wrapper. We expect
            // component attributes to always 'match' on type.
            //
            // __value => <code> = __value
            //
            // For general DOM attributes, we need to be able to create a delegate that accepts UIEventArgs
            // so we use BindMethods.SetValueHandler
            //
            // BindMethods.SetValueHandler(__value => <code> = __value, <code>) OR
            // BindMethods.SetValueHandler(__value => <code> = __value, <code>, <format>)
            //
            // For now, the way this is done isn't debuggable. But since the expression
            // passed here must be an LValue, it's probably not important.
            string changeAttributeContent = null;
            if (changeAttributeNode.BoundAttribute == null && format == null)
            {
                changeAttributeContent = $"{BlazorApi.BindMethods.SetValueHandler}(__value => {originalContent} = __value, {originalContent})";
            }
            else if (changeAttributeNode.BoundAttribute == null && format != null)
            {
                changeAttributeContent = $"{BlazorApi.BindMethods.SetValueHandler}(__value => {originalContent} = __value, {originalContent}, {format})";
            }
            else
            {
                changeAttributeContent = $"__value => {originalContent} = __value";
            }

            changeAttributeNode.Children.Clear();
            changeAttributeNode.Children.Add(new CSharpExpressionIntermediateNode()
            {
                Children =
                {
                    new IntermediateToken()
                    {
                        Content = changeAttributeContent,
                        Kind = TokenKind.CSharp
                    },
                },
            });
        }

        private bool TryParseBindAttribute(
            string attributeName,
            out string valueAttributeName,
            out string changeAttributeName)
        {
            valueAttributeName = null;
            changeAttributeName = null;

            if (!attributeName.StartsWith("bind"))
            {
                return false;
            }

            if (attributeName == "bind")
            {
                return true;
            }

            var segments = attributeName.Split('-');
            for (var i = 0; i < segments.Length; i++)
            {
                if (string.IsNullOrEmpty(segments[0]))
                {
                    return false;
                }
            }

            switch (segments.Length)
            {
                case 2:
                    valueAttributeName = segments[1];
                    return true;

                case 3:
                    changeAttributeName = segments[2];
                    valueAttributeName = segments[1];
                    return true;

                default:
                    return false;
            }
        }

        // Attempts to compute the attribute names that should be used for an instance of 'bind'.
        private bool TryComputeAttributeNames(
            TagHelperIntermediateNode node,
            string attributeName,
            out string valueAttributeName,
            out string changeAttributeName,
            out BoundAttributeDescriptor valueAttribute,
            out BoundAttributeDescriptor changeAttribute)
        {
            valueAttribute = null;
            changeAttribute = null;

            // Even though some of our 'bind' tag helpers specify the attribute names, they
            // should still satisfy one of the valid syntaxes.
            if (!TryParseBindAttribute(attributeName, out valueAttributeName, out changeAttributeName))
            {
                return false;
            }

            // The tag helper specifies attribute names, they should win.
            //
            // This handles cases like <input type="text" bind="@Foo" /> where the tag helper is 
            // generated to match a specific tag and has metadata that identify the attributes.
            //
            // We expect 1 bind tag helper per-node.
            var bindTagHelper = node.TagHelpers.Single(t => t.IsBindTagHelper());
            valueAttributeName = bindTagHelper.GetValueAttributeName() ?? valueAttributeName;
            changeAttributeName = bindTagHelper.GetChangeAttributeName() ?? changeAttributeName;

            // We expect 0-1 components per-node.
            var componentTagHelper = node.TagHelpers.FirstOrDefault(t => t.IsComponentTagHelper());
            if (componentTagHelper == null)
            {
                // If it's not a component node then there isn't too much else to figure out.
                return attributeName != null && changeAttributeName != null;
            }

            // If this is a component, we need an attribute name for the value.
            if (attributeName == null)
            {
                return false;
            }

            // If this is a component, then we can infer '<PropertyName>Changed' as the name
            // of the change event.
            if (changeAttributeName == null)
            {
                changeAttributeName = valueAttributeName + "Changed";
            }

            for (var i = 0; i < componentTagHelper.BoundAttributes.Count; i++)
            {
                var attribute = componentTagHelper.BoundAttributes[i];

                if (string.Equals(valueAttributeName, attribute.Name))
                {
                    valueAttribute = attribute;
                }

                if (string.Equals(changeAttributeName, attribute.Name))
                {
                    changeAttribute = attribute;
                }
            }

            return true;
        }

        private bool TryGetFormatNode(
            TagHelperIntermediateNode node,
            ComponentAttributeExtensionNode attributeNode,
            string valueAttributeName,
            out ComponentAttributeExtensionNode formatNode)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i] as ComponentAttributeExtensionNode;
                if (child != null &&
                    child.TagHelper != null &&
                    child.TagHelper == attributeNode.TagHelper &&
                    child.AttributeName == "format-" + valueAttributeName)
                {
                    formatNode = child;
                    return true;
                }
            }

            formatNode = null;
            return false;
        }

        private static string GetAttributeContent(ComponentAttributeExtensionNode node)
        {
            if (node.Children[0] is HtmlContentIntermediateNode htmlContentNode)
            {
                // This case can be hit for a 'string' attribute. We want to turn it into
                // an expression.
                return "\"" + ((IntermediateToken)htmlContentNode.Children.Single()).Content + "\"";
            }
            else if (node.Children[0] is CSharpExpressionIntermediateNode cSharpNode)
            {
                // This case can be hit when the attribute has an explicit @ inside, which
                // 'escapes' any special sugar we provide for codegen.
                return ((IntermediateToken)cSharpNode.Children.Single()).Content;
            }
            else
            {
                // This is the common case for 'mixed' content
                return ((IntermediateToken)node.Children.Single()).Content;
            }
        }
    }
}
