// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class BindLoweringPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run after event handler pass
        public override int Order => 100;

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
            var references = documentNode.FindDescendantReferences<TagHelperPropertyIntermediateNode>();

            var parents = new HashSet<IntermediateNode>();
            for (var i = 0; i < references.Count; i++)
            {
                parents.Add(references[i].Parent);
            }

            foreach (var parent in parents)
            {
                ProcessDuplicates(parent);
            }

            for (var i = 0; i < references.Count; i++)
            {
                var reference = references[i];
                var node = (TagHelperPropertyIntermediateNode)reference.Node;

                if (!reference.Parent.Children.Contains(node))
                {
                    // This node was removed as a duplicate, skip it.
                    continue;
                }

                if (node.TagHelper.IsBindTagHelper() && node.AttributeName.StartsWith("bind"))
                {
                    // Workaround for https://github.com/aspnet/Blazor/issues/703
                    var rewritten = RewriteUsage(reference.Parent, node);
                    reference.Remove();

                    for (var j = 0; j < rewritten.Length; j++)
                    {
                        reference.Parent.Children.Add(rewritten[j]);
                    }
                }
            }
        }

        private void ProcessDuplicates(IntermediateNode node)
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
                var attribute = node.Children[i] as TagHelperPropertyIntermediateNode;
                if (attribute != null &&
                    attribute.TagHelper != null &&
                    attribute.TagHelper.IsFallbackBindTagHelper())
                {
                    for (var j = 0; j < node.Children.Count; j++)
                    {
                        var duplicate = node.Children[j] as TagHelperPropertyIntermediateNode;
                        if (duplicate != null &&
                            duplicate.TagHelper != null &&
                            duplicate.TagHelper.IsBindTagHelper() &&
                            duplicate.AttributeName == attribute.AttributeName &&
                            !object.ReferenceEquals(attribute, duplicate))
                        {
                            // Found a duplicate - remove the 'fallback' in favor of the
                            // more specific tag helper.
                            node.Children.RemoveAt(i);
                            break;
                        }
                    }
                }

                // Also treat the general <input bind="..." /> as a 'fallback' for that case and remove it.
                // This is a workaround for a limitation where you can't write a tag helper that binds only
                // when a specific attribute is **not** present.
                if (attribute != null &&
                    attribute.TagHelper != null &&
                    attribute.TagHelper.IsInputElementFallbackBindTagHelper())
                {
                    for (var j = 0; j < node.Children.Count; j++)
                    {
                        var duplicate = node.Children[j] as TagHelperPropertyIntermediateNode;
                        if (duplicate != null &&
                            duplicate.TagHelper != null &&
                            duplicate.TagHelper.IsInputElementBindTagHelper() &&
                            duplicate.AttributeName == attribute.AttributeName &&
                            !object.ReferenceEquals(attribute, duplicate))
                        {
                            // Found a duplicate - remove the 'fallback' input tag helper in favor of the
                            // more specific tag helper.
                            node.Children.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            // If we still have duplicates at this point then they are genuine conflicts.
            var duplicates = node.Children
                .OfType<TagHelperPropertyIntermediateNode>()
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

        private IntermediateNode[] RewriteUsage(IntermediateNode parent, TagHelperPropertyIntermediateNode node)
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
                parent,
                node,
                node.AttributeName,
                out var valueAttributeName,
                out var changeAttributeName,
                out var valueAttribute,
                out var changeAttribute))
            {
                // Skip anything we can't understand. It's important that we don't crash, that will bring down
                // the build.
                node.Diagnostics.Add(BlazorDiagnosticFactory.CreateBindAttribute_InvalidSyntax(
                    node.Source, 
                    node.AttributeName));
                return new[] { node };
            }

            var original = GetAttributeContent(node);
            if (string.IsNullOrEmpty(original.Content))
            {
                // This can happen in error cases, the parser will already have flagged this
                // as an error, so ignore it.
                return new[] { node };
            }

            // Look for a matching format node. If we find one then we need to pass the format into the
            // two nodes we generate.
            IntermediateToken format = null;
            if (TryGetFormatNode(
                parent,
                node,
                valueAttributeName,
                out var formatNode))
            {
                // Don't write the format out as its own attribute, just capture it as a string
                // or expression.
                parent.Children.Remove(formatNode);
                format = GetAttributeContent(formatNode);
            }

            // Now rewrite the content of the value node to look like:
            //
            // BindMethods.GetValue(<code>) OR
            // BindMethods.GetValue(<code>, <format>)
            var valueExpressionTokens = new List<IntermediateToken>();
            valueExpressionTokens.Add(new IntermediateToken()
            {
                Content = $"{ComponentsApi.BindMethods.GetValue}(",
                Kind = TokenKind.CSharp
            });
            valueExpressionTokens.Add(original);
            if (!string.IsNullOrEmpty(format?.Content))
            {
                valueExpressionTokens.Add(new IntermediateToken()
                {
                    Content = ", ",
                    Kind = TokenKind.CSharp,
                });
                valueExpressionTokens.Add(format);
            }
            valueExpressionTokens.Add(new IntermediateToken()
            {
                Content = ")",
                Kind = TokenKind.CSharp,
            });

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
            // Note that the linemappings here are applied to the value attribute, not the change attribute.
            
            string changeExpressionContent = null;
            if (changeAttribute == null && format == null)
            {
                changeExpressionContent = $"{ComponentsApi.BindMethods.SetValueHandler}(__value => {original.Content} = __value, {original.Content})";
            }
            else if (changeAttribute == null && format != null)
            {
                changeExpressionContent = $"{ComponentsApi.BindMethods.SetValueHandler}(__value => {original.Content} = __value, {original.Content}, {format.Content})";
            }
            else
            {
                changeExpressionContent = $"__value => {original.Content} = __value";
            }
            var changeExpressionTokens = new List<IntermediateToken>()
            {
                new IntermediateToken()
                {
                    Content = changeExpressionContent,
                    Kind = TokenKind.CSharp
                }
            };

            if (parent is HtmlElementIntermediateNode)
            {
                var valueNode = new HtmlAttributeIntermediateNode()
                {
                    AttributeName = valueAttributeName,
                    Source = node.Source,

                    Prefix = valueAttributeName + "=\"",
                    Suffix = "\"",
                };

                for (var i = 0; i < node.Diagnostics.Count; i++)
                {
                    valueNode.Diagnostics.Add(node.Diagnostics[i]);
                }

                valueNode.Children.Add(new CSharpExpressionAttributeValueIntermediateNode());
                for (var i = 0; i < valueExpressionTokens.Count; i++)
                {
                    valueNode.Children[0].Children.Add(valueExpressionTokens[i]);
                }

                var changeNode = new HtmlAttributeIntermediateNode()
                {
                    AttributeName = changeAttributeName,
                    Source = node.Source,

                    Prefix = changeAttributeName + "=\"",
                    Suffix = "\"",
                };

                changeNode.Children.Add(new CSharpExpressionAttributeValueIntermediateNode());
                for (var i = 0; i < changeExpressionTokens.Count; i++)
                {
                    changeNode.Children[0].Children.Add(changeExpressionTokens[i]);
                }

                return  new[] { valueNode, changeNode };
            }
            else
            {
                var valueNode = new ComponentAttributeExtensionNode(node)
                {
                    AttributeName = valueAttributeName,
                    BoundAttribute = valueAttribute, // Might be null if it doesn't match a component attribute
                    PropertyName = valueAttribute?.GetPropertyName(),
                    TagHelper = valueAttribute == null ? null : node.TagHelper,
                    TypeName = valueAttribute?.IsWeaklyTyped() == false ? valueAttribute.TypeName : null,
                };

                valueNode.Children.Clear();
                valueNode.Children.Add(new CSharpExpressionIntermediateNode());
                for (var i = 0; i < valueExpressionTokens.Count; i++)
                {
                    valueNode.Children[0].Children.Add(valueExpressionTokens[i]);
                }

                var changeNode = new ComponentAttributeExtensionNode(node)
                {
                    AttributeName = changeAttributeName,
                    BoundAttribute = changeAttribute, // Might be null if it doesn't match a component attribute
                    PropertyName = changeAttribute?.GetPropertyName(),
                    TagHelper = changeAttribute == null ? null : node.TagHelper,
                    TypeName = changeAttribute?.IsWeaklyTyped() == false ? changeAttribute.TypeName : null,
                };

                changeNode.Children.Clear();
                changeNode.Children.Add(new CSharpExpressionIntermediateNode());
                for (var i = 0; i < changeExpressionTokens.Count; i++)
                {
                    changeNode.Children[0].Children.Add(changeExpressionTokens[i]);
                }

                return new[] { valueNode, changeNode };
            }
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
                if (string.IsNullOrEmpty(segments[i]))
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
            IntermediateNode parent,
            TagHelperPropertyIntermediateNode node,
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
            valueAttributeName = node.TagHelper.GetValueAttributeName() ?? valueAttributeName;
            changeAttributeName = node.TagHelper.GetChangeAttributeName() ?? changeAttributeName;

            // We expect 0-1 components per-node.
            var componentTagHelper = (parent as ComponentExtensionNode)?.Component;
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
            IntermediateNode node,
            TagHelperPropertyIntermediateNode attributeNode,
            string valueAttributeName,
            out TagHelperPropertyIntermediateNode formatNode)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i] as TagHelperPropertyIntermediateNode;
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

        private static IntermediateToken GetAttributeContent(TagHelperPropertyIntermediateNode node)
        {
            var template = node.FindDescendantNodes<TemplateIntermediateNode>().FirstOrDefault();
            if (template != null)
            {
                // See comments in TemplateDiagnosticPass
                node.Diagnostics.Add(BlazorDiagnosticFactory.Create_TemplateInvalidLocation(template.Source));
                return new IntermediateToken() { Kind = TokenKind.CSharp, Content = string.Empty, };
            }

            if (node.Children[0] is HtmlContentIntermediateNode htmlContentNode)
            {
                // This case can be hit for a 'string' attribute. We want to turn it into
                // an expression.
                var content = "\"" + string.Join(string.Empty, htmlContentNode.Children.OfType<IntermediateToken>().Select(t => t.Content)) + "\"";
                return new IntermediateToken() { Kind = TokenKind.CSharp, Content = content };
            }
            else if (node.Children[0] is CSharpExpressionIntermediateNode cSharpNode)
            {
                // This case can be hit when the attribute has an explicit @ inside, which
                // 'escapes' any special sugar we provide for codegen.
                return GetToken(cSharpNode);
            }
            else
            {
                // This is the common case for 'mixed' content
                return GetToken(node);
            }

            // In error cases we won't have a single token, but we still want to generate the code.
            IntermediateToken GetToken(IntermediateNode parent)
            {
                return
                    parent.Children.Count == 1 ? (IntermediateToken)parent.Children[0] : new IntermediateToken()
                    {
                        Kind = TokenKind.CSharp,
                        Content = string.Join(string.Empty, parent.Children.OfType<IntermediateToken>().Select(t => t.Content)),
                    };
            }
        }
    }
}
