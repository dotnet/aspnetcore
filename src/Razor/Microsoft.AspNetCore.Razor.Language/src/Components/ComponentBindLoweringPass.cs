// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentBindLoweringPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Run after event handler pass
    public override int Order => 100;

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

        // For each @bind *usage* we need to rewrite the tag helper node to map to basic constructs.
        var references = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeIntermediateNode>();
        var parameterReferences = documentNode.FindDescendantReferences<TagHelperDirectiveAttributeParameterIntermediateNode>();

        var parents = new HashSet<IntermediateNode>();
        for (var i = 0; i < references.Count; i++)
        {
            parents.Add(references[i].Parent);
        }
        for (var i = 0; i < parameterReferences.Count; i++)
        {
            parents.Add(parameterReferences[i].Parent);
        }

        foreach (var parent in parents)
        {
            ProcessDuplicates(parent);
        }

        // First, collect all the non-parameterized @bind or @bind-* attributes.
        // The dict key is a tuple of (parent, attributeName) to differentiate attributes with the same name in two different elements.
        // We don't have to worry about duplicate bound attributes in the same element
        // like, <Foo @bind="bar" @bind="bar" />, because IR lowering takes care of that.
        var bindEntries = new Dictionary<(IntermediateNode, string), BindEntry>();
        for (var i = 0; i < references.Count; i++)
        {
            var reference = references[i];
            var parent = reference.Parent;
            var node = (TagHelperDirectiveAttributeIntermediateNode)reference.Node;

            if (!parent.Children.Contains(node))
            {
                // This node was removed as a duplicate, skip it.
                continue;
            }

            if (node.TagHelper.IsBindTagHelper())
            {
                bindEntries[(parent, node.AttributeName)] = new BindEntry(reference);
            }
        }

        // Now collect all the parameterized attributes and store them along with their corresponding @bind or @bind-* attributes.
        for (var i = 0; i < parameterReferences.Count; i++)
        {
            var parameterReference = parameterReferences[i];
            var parent = parameterReference.Parent;
            var node = (TagHelperDirectiveAttributeParameterIntermediateNode)parameterReference.Node;

            if (!parent.Children.Contains(node))
            {
                // This node was removed as a duplicate, skip it.
                continue;
            }

            if (node.TagHelper.IsBindTagHelper())
            {
                // Check if this tag contains a corresponding non-parameterized bind node.
                if (!bindEntries.TryGetValue((parent, node.AttributeNameWithoutParameter), out var entry))
                {
                    // There is no corresponding bind node. Add a diagnostic and move on.
                    parameterReference.Parent.Diagnostics.Add(ComponentDiagnosticFactory.CreateBindAttributeParameter_MissingBind(
                        node.Source,
                        node.AttributeName));
                }
                else if (node.BoundAttributeParameter.Name == "event")
                {
                    entry.BindEventNode = node;
                }
                else if (node.BoundAttributeParameter.Name == "format")
                {
                    entry.BindFormatNode = node;
                }
                else if (node.BoundAttributeParameter.Name == "culture")
                {
                    entry.BindCultureNode = node;
                }
                else
                {
                    // Unsupported bind attribute parameter. This can only happen if bound attribute descriptor
                    // is configured to expect a parameter other than 'event' and 'format'.
                }

                // We've extracted what we need from the parameterized bind node. Remove it.
                parameterReference.Remove();
            }
        }

        // We now have all the info we need to rewrite the tag helper.
        foreach (var entry in bindEntries)
        {
            var reference = entry.Value.BindNodeReference;
            var rewritten = RewriteUsage(reference.Parent, entry.Value);
            reference.Remove();

            for (var j = 0; j < rewritten.Length; j++)
            {
                reference.Parent.Children.Add(rewritten[j]);
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
            TagHelperDescriptor tagHelper = null;
            string attributeName = null;
            var attribute = node.Children[i];
            if (attribute is TagHelperDirectiveAttributeIntermediateNode directiveAttribute)
            {
                attributeName = directiveAttribute.AttributeName;
                tagHelper = directiveAttribute.TagHelper;
            }
            else if (attribute is TagHelperDirectiveAttributeParameterIntermediateNode parameterAttribute)
            {
                attributeName = parameterAttribute.AttributeName;
                tagHelper = parameterAttribute.TagHelper;
            }
            if (attribute != null &&
                tagHelper != null &&
                tagHelper.IsFallbackBindTagHelper())
            {
                for (var j = 0; j < node.Children.Count; j++)
                {
                    TagHelperDescriptor duplicateTagHelper = null;
                    string duplicateAttributeName = null;
                    var duplicate = node.Children[j];
                    if (duplicate is TagHelperDirectiveAttributeIntermediateNode duplicateDirectiveAttribute)
                    {
                        duplicateAttributeName = duplicateDirectiveAttribute.AttributeName;
                        duplicateTagHelper = duplicateDirectiveAttribute.TagHelper;
                    }
                    else if (duplicate is TagHelperDirectiveAttributeParameterIntermediateNode duplicateParameterAttribute)
                    {
                        duplicateAttributeName = duplicateParameterAttribute.AttributeName;
                        duplicateTagHelper = duplicateParameterAttribute.TagHelper;
                    }
                    if (duplicate != null &&
                        duplicateTagHelper != null &&
                        duplicateTagHelper.IsBindTagHelper() &&
                        duplicateAttributeName == attributeName &&
                        !object.ReferenceEquals(attribute, duplicate))
                    {
                        // Found a duplicate - remove the 'fallback' in favor of the
                        // more specific tag helper.
                        node.Children.RemoveAt(i);
                        break;
                    }
                }
            }

            // Also treat the general <input @bind="..." /> as a 'fallback' for that case and remove it.
            // This is a workaround for a limitation where you can't write a tag helper that binds only
            // when a specific attribute is **not** present.
            if (attribute != null &&
                tagHelper != null &&
                tagHelper.IsInputElementFallbackBindTagHelper())
            {
                for (var j = 0; j < node.Children.Count; j++)
                {
                    TagHelperDescriptor duplicateTagHelper = null;
                    string duplicateAttributeName = null;
                    var duplicate = node.Children[j];
                    if (duplicate is TagHelperDirectiveAttributeIntermediateNode duplicateDirectiveAttribute)
                    {
                        duplicateAttributeName = duplicateDirectiveAttribute.AttributeName;
                        duplicateTagHelper = duplicateDirectiveAttribute.TagHelper;
                    }
                    else if (duplicate is TagHelperDirectiveAttributeParameterIntermediateNode duplicateParameterAttribute)
                    {
                        duplicateAttributeName = duplicateParameterAttribute.AttributeName;
                        duplicateTagHelper = duplicateParameterAttribute.TagHelper;
                    }
                    if (duplicate != null &&
                        duplicateTagHelper != null &&
                        duplicateTagHelper.IsInputElementBindTagHelper() &&
                        duplicateAttributeName == attributeName &&
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
            .OfType<TagHelperDirectiveAttributeIntermediateNode>()
            .GroupBy(p => p.AttributeName)
            .Where(g => g.Count() > 1);

        foreach (var duplicate in duplicates)
        {
            node.Diagnostics.Add(ComponentDiagnosticFactory.CreateBindAttribute_Duplicates(
                node.Source,
                duplicate.First().OriginalAttributeName,
                duplicate.ToArray()));
            foreach (var property in duplicate)
            {
                node.Children.Remove(property);
            }
        }
    }

    private IntermediateNode[] RewriteUsage(IntermediateNode parent, BindEntry bindEntry)
    {
        // Bind works similarly to a macro, it always expands to code that the user could have written.
        //
        // For the nodes that are related to the bind-attribute rewrite them to look like a set of
        // 'normal' HTML attributes similar to the following transformation.
        //
        // Input:   <MyComponent @bind-Value="@currentCount" />
        // Output:  <MyComponent Value ="...<get the value>..." ValueChanged ="... <set the value>..." ValueExpression ="() => ...<get the value>..." />
        //
        // This means that the expression that appears inside of '@bind' must be an LValue or else
        // there will be errors. In general the errors that come from C# in this case are good enough
        // to understand the problem.
        //
        // We also support and encourage the use of EventCallback<> with bind. So in the above example
        // the ValueChanged property could be an Action<> or an EventCallback<>.
        //
        // The BindMethods calls are required with Action<> because to give us a good experience. They
        // use overloading to ensure that can get an Action<object> that will convert and set an arbitrary
        // value. We have a similar set of APIs to use with EventCallback<>.
        //
        // We also assume that the element will be treated as a component for now because
        // multiple passes handle 'special' tag helpers. We have another pass that translates
        // a tag helper node back into 'regular' element when it doesn't have an associated component
        var node = bindEntry.BindNode;
        if (!TryComputeAttributeNames(
            parent,
            bindEntry,
            out var valueAttributeName,
            out var changeAttributeName,
            out var expressionAttributeName,
            out var changeAttributeNode,
            out var valueAttribute,
            out var changeAttribute,
            out var expressionAttribute))
        {
            // Skip anything we can't understand. It's important that we don't crash, that will bring down
            // the build.
            node.Diagnostics.Add(ComponentDiagnosticFactory.CreateBindAttribute_InvalidSyntax(
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

        // Look for a format. If we find one then we need to pass the format into the
        // two nodes we generate.
        IntermediateToken format = null;
        if (bindEntry.BindFormatNode != null)
        {
            format = GetAttributeContent(bindEntry.BindFormatNode);
        }
        else if (node.TagHelper?.GetFormat() != null)
        {
            // We may have a default format if one is associated with the field type.
            format = new IntermediateToken()
            {
                Kind = TokenKind.CSharp,
                Content = "\"" + node.TagHelper.GetFormat() + "\"",
            };
        }

        // Look for a culture. If we find one then we need to pass the culture into the
        // two nodes we generate.
        IntermediateToken culture = null;
        if (bindEntry.BindCultureNode != null)
        {
            culture = GetAttributeContent(bindEntry.BindCultureNode);
        }
        else if (node.TagHelper?.IsInvariantCultureBindTagHelper() == true)
        {
            // We may have a default invariant culture if one is associated with the field type.
            culture = new IntermediateToken()
            {
                Kind = TokenKind.CSharp,
                Content = $"global::{typeof(CultureInfo).FullName}.{nameof(CultureInfo.InvariantCulture)}",
            };
        }

        var valueExpressionTokens = new List<IntermediateToken>();
        var changeExpressionTokens = new List<IntermediateToken>();

        // There are a few cases to handle for @bind:
        // 1. This is a component using a delegate (int Value & Action<int> Value)
        // 2. This is a component using EventCallback (int value & EventCallback<int>)
        // 3. This is an element
        if (parent is ComponentIntermediateNode && changeAttribute != null && changeAttribute.IsDelegateProperty())
        {
            RewriteNodesForComponentDelegateBind(
                original,
                valueExpressionTokens,
                changeExpressionTokens);
        }
        else if (parent is ComponentIntermediateNode)
        {
            RewriteNodesForComponentEventCallbackBind(
                original,
                valueExpressionTokens,
                changeExpressionTokens);
        }
        else
        {
            RewriteNodesForElementEventCallbackBind(
                original,
                format,
                culture,
                valueExpressionTokens,
                changeExpressionTokens);
        }

        if (parent is MarkupElementIntermediateNode)
        {
            var valueNode = new HtmlAttributeIntermediateNode()
            {
                Annotations =
                    {
                        [ComponentMetadata.Common.OriginalAttributeName] = node.OriginalAttributeName,
                    },
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
                Annotations =
                    {
                        [ComponentMetadata.Common.OriginalAttributeName] = node.OriginalAttributeName,
                    },
                AttributeName = changeAttributeName,
                AttributeNameExpression = changeAttributeNode,
                Source = node.Source,

                Prefix = changeAttributeName + "=\"",
                Suffix = "\"",

                EventUpdatesAttributeName = valueAttributeName,
            };

            changeNode.Children.Add(new CSharpExpressionAttributeValueIntermediateNode());
            for (var i = 0; i < changeExpressionTokens.Count; i++)
            {
                changeNode.Children[0].Children.Add(changeExpressionTokens[i]);
            }

            return new[] { valueNode, changeNode };
        }
        else
        {
            var valueNode = new ComponentAttributeIntermediateNode(node)
            {
                Annotations =
                    {
                        [ComponentMetadata.Common.OriginalAttributeName] = node.OriginalAttributeName,
                    },
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

            var changeNode = new ComponentAttributeIntermediateNode(node)
            {
                Annotations =
                    {
                        [ComponentMetadata.Common.OriginalAttributeName] = node.OriginalAttributeName,
                    },
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

            // Finally, also emit a node for the "Expression" attribute, but only if the target
            // component is defined to accept one
            ComponentAttributeIntermediateNode expressionNode = null;
            if (expressionAttribute != null)
            {
                expressionNode = new ComponentAttributeIntermediateNode(node)
                {
                    Annotations =
                        {
                            [ComponentMetadata.Common.OriginalAttributeName] = node.OriginalAttributeName,
                        },
                    AttributeName = expressionAttributeName,
                    BoundAttribute = expressionAttribute,
                    PropertyName = expressionAttribute.GetPropertyName(),
                    TagHelper = node.TagHelper,
                    TypeName = expressionAttribute.IsWeaklyTyped() ? null : expressionAttribute.TypeName,
                };

                expressionNode.Children.Clear();
                expressionNode.Children.Add(new CSharpExpressionIntermediateNode());
                expressionNode.Children[0].Children.Add(new IntermediateToken()
                {
                    Content = $"() => {original.Content}",
                    Kind = TokenKind.CSharp
                });
            }

            return expressionNode == null
                ? new[] { valueNode, changeNode }
                : new[] { valueNode, changeNode, expressionNode };
        }
    }

    private bool TryParseBindAttribute(BindEntry bindEntry, out string valueAttributeName)
    {
        var attributeName = bindEntry.BindNode.AttributeName;
        valueAttributeName = null;

        if (attributeName == "bind")
        {
            return true;
        }

        if (!attributeName.StartsWith("bind-", StringComparison.Ordinal))
        {
            return false;
        }

        valueAttributeName = attributeName.Substring("bind-".Length);
        return true;
    }

    // Attempts to compute the attribute names that should be used for an instance of 'bind'.
    private bool TryComputeAttributeNames(
        IntermediateNode parent,
        BindEntry bindEntry,
        out string valueAttributeName,
        out string changeAttributeName,
        out string expressionAttributeName,
        out CSharpExpressionIntermediateNode changeAttributeNode,
        out BoundAttributeDescriptor valueAttribute,
        out BoundAttributeDescriptor changeAttribute,
        out BoundAttributeDescriptor expressionAttribute)
    {
        changeAttributeName = null;
        expressionAttributeName = null;
        changeAttributeNode = null;
        valueAttribute = null;
        changeAttribute = null;
        expressionAttribute = null;

        // The tag helper specifies attribute names, they should win.
        //
        // This handles cases like <input type="text" bind="@Foo" /> where the tag helper is
        // generated to match a specific tag and has metadata that identify the attributes.
        //
        // We expect 1 bind tag helper per-node.
        var node = bindEntry.BindNode;
        var attributeName = node.AttributeName;

        // Even though some of our 'bind' tag helpers specify the attribute names, they
        // should still satisfy one of the valid syntaxes.
        if (!TryParseBindAttribute(bindEntry, out valueAttributeName))
        {
            return false;
        }

        valueAttributeName = node.TagHelper.GetValueAttributeName() ?? valueAttributeName;

        // If there an attribute that specifies the event like @bind:event="oninput",
        // that should be preferred. Otherwise, use the one from the tag helper.
        if (bindEntry.BindEventNode == null)
        {
            // @bind:event not specified
            changeAttributeName = node.TagHelper.GetChangeAttributeName();
        }
        else if (TryExtractEventNodeStaticText(bindEntry.BindEventNode, out var text))
        {
            // @bind:event="oninput" - change attribute is static
            changeAttributeName = text;
        }
        else
        {
            // @bind:event="@someExpr" - we can't know the name of the change attribute, it's dynamic
            changeAttributeNode = ExtractEventNodeExpression(bindEntry.BindEventNode);
        }

        expressionAttributeName = node.TagHelper.GetExpressionAttributeName();

        // We expect 0-1 components per-node.
        var componentTagHelper = (parent as ComponentIntermediateNode)?.Component;
        if (componentTagHelper == null)
        {
            // If it's not a component node then there isn't too much else to figure out.
            return attributeName != null && (changeAttributeName != null || changeAttributeNode != null);
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

        // Likewise for the expression attribute
        if (expressionAttributeName == null)
        {
            expressionAttributeName = valueAttributeName + "Expression";
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

            if (string.Equals(expressionAttributeName, attribute.Name))
            {
                expressionAttribute = attribute;
            }
        }

        return true;

        static bool TryExtractEventNodeStaticText(TagHelperDirectiveAttributeParameterIntermediateNode node, out string text)
        {
            if (node.Children[0] is HtmlContentIntermediateNode html)
            {
                text = GetAttributeContent(html).Content;
                return true;
            }

            text = null;
            return false;
        }

        static CSharpExpressionIntermediateNode ExtractEventNodeExpression(TagHelperDirectiveAttributeParameterIntermediateNode node)
        {
            if (node.Children[0] is CSharpExpressionIntermediateNode expression)
            {
                return expression;
            }

            return null;
        }
    }

    private void RewriteNodesForComponentDelegateBind(
        IntermediateToken original,
        List<IntermediateToken> valueExpressionTokens,
        List<IntermediateToken> changeExpressionTokens)
    {
        // For a component using @bind we want to:
        //  - use the value as-is
        //  - create a delegate to handle changes
        valueExpressionTokens.Add(original);

        // Now rewrite the content of the change-handler node. Since it's a component attribute,
        // we don't use the 'BindMethods' wrapper. We expect component attributes to always 'match' on type.
        //
        // __value => <code> = __value
        changeExpressionTokens.Add(new IntermediateToken()
        {
            Content = $"__value => {original.Content} = __value",
            Kind = TokenKind.CSharp,
        });
    }

    private void RewriteNodesForComponentEventCallbackBind(
        IntermediateToken original,
        List<IntermediateToken> valueExpressionTokens,
        List<IntermediateToken> changeExpressionTokens)
    {
        // For a component using @bind we want to:
        //  - use the value as-is
        //  - create a delegate to handle changes
        valueExpressionTokens.Add(original);

        changeExpressionTokens.Add(new IntermediateToken()
        {
            Content = $"{ComponentsApi.RuntimeHelpers.CreateInferredEventCallback}(this, __value => {original.Content} = __value, {original.Content})",
            Kind = TokenKind.CSharp
        });
    }

    private void RewriteNodesForElementEventCallbackBind(
        IntermediateToken original,
        IntermediateToken format,
        IntermediateToken culture,
        List<IntermediateToken> valueExpressionTokens,
        List<IntermediateToken> changeExpressionTokens)
    {
        // This is bind on a markup element. We use FormatValue to transform the value in the correct way
        // according to format and culture.
        //
        // Now rewrite the content of the value node to look like:
        //
        // BindConverter.FormatValue(<code>, format: <format>, culture: <culture>)
        valueExpressionTokens.Add(new IntermediateToken()
        {
            Content = $"{ComponentsApi.BindConverter.FormatValue}(",
            Kind = TokenKind.CSharp
        });
        valueExpressionTokens.Add(original);

        if (!string.IsNullOrEmpty(format?.Content))
        {
            valueExpressionTokens.Add(new IntermediateToken()
            {
                Content = ", format: ",
                Kind = TokenKind.CSharp,
            });
            valueExpressionTokens.Add(format);
        }

        if (!string.IsNullOrEmpty(culture?.Content))
        {
            valueExpressionTokens.Add(new IntermediateToken()
            {
                Content = ", culture: ",
                Kind = TokenKind.CSharp,
            });
            valueExpressionTokens.Add(culture);
        }

        valueExpressionTokens.Add(new IntermediateToken()
        {
            Content = ")",
            Kind = TokenKind.CSharp,
        });

        // Now rewrite the content of the change-handler node. There are two cases we care about
        // here. If it's a component attribute, then don't use the 'CreateBinder' wrapper. We expect
        // component attributes to always 'match' on type.
        //
        // The really tricky part of this is that we CANNOT write the type name of of the EventCallback we
        // intend to create. Doing so would really complicate the story for how we deal with generic types,
        // since the generic type lowering pass runs after this. To keep this simple we're relying on
        // the compiler to resolve overloads for us.
        //
        // RuntimeHelpers.CreateInferredEventCallback(this, __value => <code> = __value, <code>)
        //
        // For general DOM attributes, we need to be able to create a delegate that accepts UIEventArgs
        // so we use 'CreateBinder'
        //
        // EventCallbackFactory.CreateBinder(this, __value => <code> = __value, <code>, format: <format>, culture: <culture>)
        //
        // Note that the linemappings here are applied to the value attribute, not the change attribute.
        changeExpressionTokens.Add(new IntermediateToken()
        {
            Content = $"{ComponentsApi.EventCallback.FactoryAccessor}.{ComponentsApi.EventCallbackFactory.CreateBinderMethod}(this, __value => {original.Content} = __value, ",
            Kind = TokenKind.CSharp
        });

        changeExpressionTokens.Add(new IntermediateToken()
        {
            Content = original.Content,
            Kind = TokenKind.CSharp
        });

        if (format != null)
        {
            changeExpressionTokens.Add(new IntermediateToken()
            {
                Content = $", format: {format.Content}",
                Kind = TokenKind.CSharp
            });
        }

        if (culture != null)
        {
            changeExpressionTokens.Add(new IntermediateToken()
            {
                Content = $", culture: {culture.Content}",
                Kind = TokenKind.CSharp
            });
        }

        changeExpressionTokens.Add(new IntermediateToken()
        {
            Content = ")",
            Kind = TokenKind.CSharp,
        });
    }

    private static IntermediateToken GetAttributeContent(IntermediateNode node)
    {
        var nodes = node.FindDescendantNodes<TemplateIntermediateNode>();
        var template = nodes.Count > 0 ? nodes[0] : default;
        if (template != null)
        {
            // See comments in TemplateDiagnosticPass
            node.Diagnostics.Add(ComponentDiagnosticFactory.Create_TemplateInvalidLocation(template.Source));
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

        IntermediateToken GetToken(IntermediateNode parent)
        {
            if (parent.Children.Count == 1 && parent.Children[0] is IntermediateToken token)
            {
                return token;
            }

            // In error cases we won't have a single token, but we still want to generate the code.
            return new IntermediateToken()
            {
                Kind = TokenKind.CSharp,
                Content = string.Join(string.Empty, parent.Children.OfType<IntermediateToken>().Select(t => t.Content)),
            };
        }
    }

    private class BindEntry
    {
        public BindEntry(IntermediateNodeReference bindNodeReference)
        {
            BindNodeReference = bindNodeReference;
            BindNode = (TagHelperDirectiveAttributeIntermediateNode)bindNodeReference.Node;
        }

        public IntermediateNodeReference BindNodeReference { get; }

        public TagHelperDirectiveAttributeIntermediateNode BindNode { get; }

        public TagHelperDirectiveAttributeParameterIntermediateNode BindEventNode { get; set; }

        public TagHelperDirectiveAttributeParameterIntermediateNode BindFormatNode { get; set; }

        public TagHelperDirectiveAttributeParameterIntermediateNode BindCultureNode { get; set; }
    }
}
