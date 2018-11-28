// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class ComponentLoweringPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // This pass runs earlier than our other passes that 'lower' specific kinds of attributes.
        public override int Order => 0;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                // Nothing to do, bail. We can't function without the standard structure.
                return;
            }

            // For each component *usage* we need to rewrite the tag helper node to map to the relevant component
            // APIs.
            var references = documentNode.FindDescendantReferences<TagHelperIntermediateNode>();
            for (var i = 0; i < references.Count; i++)
            {
                var reference = references[i];
                var node = (TagHelperIntermediateNode)reference.Node;

                var count = 0;
                for (var j = 0; j < node.TagHelpers.Count; j++)
                {
                    if (node.TagHelpers[j].IsComponentTagHelper())
                    {
                        // Only allow a single component tag helper per element. If there are multiple, we'll just consider
                        // the first one and ignore the others.
                        if (count++ > 1)
                        {
                            node.Diagnostics.Add(BlazorDiagnosticFactory.Create_MultipleComponents(node.Source, node.TagName, node.TagHelpers));
                            break;
                        }
                    }
                }

                if (count >= 1)
                {
                    reference.Replace(RewriteAsComponent(node, node.TagHelpers.First(t => t.IsComponentTagHelper())));
                }
                else if (node.TagHelpers.Any(t => t.IsChildContentTagHelper()))
                {
                    // Ignore, this will be handled when we rewrite the parent.
                }
                else
                {
                    reference.Replace(RewriteAsElement(node));
                }
            }
        }

        private ComponentExtensionNode RewriteAsComponent(TagHelperIntermediateNode node, TagHelperDescriptor tagHelper)
        {
            var component = new ComponentExtensionNode()
            {
                Component = tagHelper,
                Source = node.Source,
                TagName = node.TagName,
                TypeName = tagHelper.GetTypeName(),
            };

            for (var i = 0; i < node.Diagnostics.Count; i++)
            {
                component.Diagnostics.Add(node.Diagnostics[i]);
            }

            var visitor = new ComponentRewriteVisitor(component);
            visitor.Visit(node);

            // Fixup the parameter names of child content elements. We can't do this during the rewrite
            // because we see the nodes in the wrong order.
            foreach (var childContent in component.ChildContents)
            {
                childContent.ParameterName = childContent.ParameterName ?? component.ChildContentParameterName ?? BlazorMetadata.ChildContent.DefaultParameterName;
            }

            return component;
        }

        private HtmlElementIntermediateNode RewriteAsElement(TagHelperIntermediateNode node)
        {
            var result = new HtmlElementIntermediateNode()
            {
                Source = node.Source,
                TagName = node.TagName,
            };

            for (var i = 0; i < node.Diagnostics.Count; i++)
            {
                result.Diagnostics.Add(node.Diagnostics[i]);
            }

            var visitor = new ElementRewriteVisitor(result.Children);
            visitor.Visit(node);

            return result;
        }

        private class ComponentRewriteVisitor : IntermediateNodeWalker
        {
            private readonly ComponentExtensionNode _component;
            private readonly IntermediateNodeCollection _children;

            public ComponentRewriteVisitor(ComponentExtensionNode component)
            {
                _component = component;
                _children = component.Children;
            }

            public override void VisitTagHelper(TagHelperIntermediateNode node)
            {
                // Visit children, we're replacing this node.
                base.VisitDefault(node);
            }

            public override void VisitTagHelperBody(TagHelperBodyIntermediateNode node)
            {
                // Wrap the component's children in a ChildContent node if we have some significant
                // content.
                if (node.Children.Count == 0)
                {
                    return;
                }

                // If we get a single HTML content node containing only whitespace,
                // then this is probably a tag that looks like '<MyComponent>  </MyComponent>
                //
                // We don't want to create a child content for this case, because it can conflict
                // with a child content that's set via an attribute. We don't want the formatting
                // of insignificant whitespace to be annoying when setting attributes directly.
                if (node.Children.Count == 1 && IsIgnorableWhitespace(node.Children[0]))
                {
                    return;
                }

                // From here we fork and behave differently based on whether the component's child content is
                // implicit or explicit.
                //
                // Explicit child content will look like: <MyComponent><ChildContent><div>...</div></ChildContent></MyComponent>
                // compared with implicit: <MyComponent><div></div></MyComponent>
                //
                // Using implicit child content:
                // 1. All content is grouped into a single child content lambda, and assigned to the property 'ChildContent'
                //
                // Using explicit child content:
                // 1. All content must be contained within 'child content' elements that are direct children
                // 2. Whitespace outside of 'child content' elements will be ignored (not an error)
                // 3. Non-whitespace outside of 'child content' elements will cause an error
                // 4. All 'child content' elements must match parameters on the component (exception for ChildContent,
                //    which is always allowed.
                // 5. Each 'child content' element will generate its own lambda, and be assigned to the property
                //    that matches the element name.
                if (!node.Children.OfType<TagHelperIntermediateNode>().Any(t => t.TagHelpers.Any(th => th.IsChildContentTagHelper())))
                {
                    // This node has implicit child content. It may or may not have an attribute that matches.
                    var attribute = _component.Component.BoundAttributes
                        .Where(a => string.Equals(a.Name, ComponentsApi.RenderTreeBuilder.ChildContent, StringComparison.Ordinal))
                        .FirstOrDefault();
                    _children.Add(RewriteChildContent(attribute, node.Source, node.Children));
                    return;
                }

                // OK this node has explicit child content, we can rewrite it by visiting each node
                // in sequence, since we:
                // a) need to rewrite each child content element
                // b) any significant content outside of a child content is an error
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    if (IsIgnorableWhitespace(child))
                    {
                        continue;
                    }

                    if (child is TagHelperIntermediateNode tagHelperNode &&
                        tagHelperNode.TagHelpers.Any(th => th.IsChildContentTagHelper()))
                    {
                        // This is a child content element
                        var attribute = _component.Component.BoundAttributes
                            .Where(a => string.Equals(a.Name, tagHelperNode.TagName, StringComparison.Ordinal))
                            .FirstOrDefault();
                        _children.Add(RewriteChildContent(attribute, child.Source, child.Children));
                        continue;
                    }
                    
                    // If we get here then this is significant content inside a component with explicit child content.
                    child.Diagnostics.Add(BlazorDiagnosticFactory.Create_ChildContentMixedWithExplicitChildContent(child.Source, _component));
                    _children.Add(child);
                }

                bool IsIgnorableWhitespace(IntermediateNode n)
                {
                    if (n is HtmlContentIntermediateNode html &&
                        html.Children.Count == 1 &&
                        html.Children[0] is IntermediateToken token &&
                        string.IsNullOrWhiteSpace(token.Content))
                    {
                        return true;
                    }

                    return false;
                }
            }

            private ComponentChildContentIntermediateNode RewriteChildContent(BoundAttributeDescriptor attribute, SourceSpan? source, IntermediateNodeCollection children)
            {
                var childContent = new ComponentChildContentIntermediateNode()
                {
                    BoundAttribute = attribute,
                    Source = source,
                    TypeName = attribute?.TypeName ?? ComponentsApi.RenderFragment.FullTypeName,
                };

                // There are two cases here:
                // 1. Implicit child content - the children will be non-taghelper nodes, just accept them
                // 2. Explicit child content - the children will be various tag helper nodes, that need special processing.
                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (child is TagHelperBodyIntermediateNode body)
                    {
                        // The body is all of the content we want to render, the rest of the children will
                        // be the attributes.
                        for (var j = 0; j < body.Children.Count; j++)
                        {
                            childContent.Children.Add(body.Children[j]);
                        }
                    }
                    else if (child is TagHelperPropertyIntermediateNode property)
                    {
                        if (property.BoundAttribute.IsChildContentParameterNameProperty())
                        {
                            // Check for each child content with a parameter name, that the parameter name is specified
                            // with literal text. For instance, the following is not allowed and should generate a diagnostic.
                            //
                            // <MyComponent><ChildContent Context="@Foo()">...</ChildContent></MyComponent>
                            if (TryGetAttributeStringContent(property, out var parameterName))
                            {
                                childContent.ParameterName = parameterName;
                                continue;
                            }

                            // The parameter name is invalid.
                            childContent.Diagnostics.Add(BlazorDiagnosticFactory.Create_ChildContentHasInvalidParameter(property.Source, property.AttributeName, attribute.Name));
                            continue;
                        }

                        // This is an unrecognized attribute, this is possible if you try to do something like put 'ref' on a child content.
                        childContent.Diagnostics.Add(BlazorDiagnosticFactory.Create_ChildContentHasInvalidAttribute(property.Source, property.AttributeName, attribute.Name));
                    }
                    else if (child is TagHelperHtmlAttributeIntermediateNode a)
                    {
                        // This is an HTML attribute on a child content.
                        childContent.Diagnostics.Add(BlazorDiagnosticFactory.Create_ChildContentHasInvalidAttribute(a.Source, a.AttributeName, attribute.Name));
                    }
                    else
                    {
                        // This is some other kind of node (likely an implicit child content)
                        childContent.Children.Add(child);
                    }
                }

                return childContent;
            }

            private bool TryGetAttributeStringContent(TagHelperPropertyIntermediateNode property, out string content)
            {
                // The success path looks like - a single HTML Attribute Value node with tokens
                if (property.Children.Count == 1 &&
                    property.Children[0] is HtmlContentIntermediateNode html)
                {
                    content = string.Join(string.Empty, html.Children.OfType<IntermediateToken>().Select(n => n.Content));
                    return true;
                }

                content = null;
                return false;
            }

            public override void VisitTagHelperHtmlAttribute(TagHelperHtmlAttributeIntermediateNode node)
            {
                var attribute = new ComponentAttributeExtensionNode(node);
                _children.Add(attribute);

                // Since we don't support complex content, we can rewrite the inside of this
                // node to the rather simpler form that property nodes usually have.
                for (var i = 0; i < attribute.Children.Count; i++)
                {
                    if (attribute.Children[i] is HtmlAttributeValueIntermediateNode htmlValue)
                    {
                        attribute.Children[i] = new HtmlContentIntermediateNode()
                        {
                            Children =
                            {
                                htmlValue.Children.Single(),
                            },
                            Source = htmlValue.Source,
                        };
                    }
                    else if (attribute.Children[i] is CSharpExpressionAttributeValueIntermediateNode expressionValue)
                    {
                        attribute.Children[i] = new CSharpExpressionIntermediateNode()
                        {
                            Children =
                            {
                                expressionValue.Children.Single(),
                            },
                            Source = expressionValue.Source,
                        };
                    }
                    else if (attribute.Children[i] is CSharpCodeAttributeValueIntermediateNode codeValue)
                    {
                        attribute.Children[i] = new CSharpExpressionIntermediateNode()
                        {
                            Children =
                            {
                                codeValue.Children.Single(),
                            },
                            Source = codeValue.Source,
                        };
                    }
                }
            }

            public override void VisitTagHelperProperty(TagHelperPropertyIntermediateNode node)
            {
                // Each 'tag helper property' belongs to a specific tag helper. We want to handle
                // the cases for components, but leave others alone. This allows our other passes
                // to handle those cases.
                if (!node.TagHelper.IsComponentTagHelper())
                {
                    _children.Add(node);
                    return;
                }

                // Another special case here - this might be a type argument. These don't represent 'real' parameters
                // that get passed to the component, it needs special code generation support.
                if (node.TagHelper.IsGenericTypedComponent() && node.BoundAttribute.IsTypeParameterProperty())
                {
                    _children.Add(new ComponentTypeArgumentExtensionNode(node));
                    return;
                }

                // Another special case here -- this might be a 'Context' parameter, which specifies the name
                // for lambda parameter for parameterized child content
                if (node.BoundAttribute.IsChildContentParameterNameProperty())
                {
                    // Check for each child content with a parameter name, that the parameter name is specified
                    // with literal text. For instance, the following is not allowed and should generate a diagnostic.
                    //
                    // <MyComponent Context="@Foo()">...</MyComponent>
                    if (TryGetAttributeStringContent(node, out var parameterName))
                    {
                        _component.ChildContentParameterName = parameterName;
                        return;
                    }

                    // The parameter name is invalid.
                    _component.Diagnostics.Add(BlazorDiagnosticFactory.Create_ChildContentHasInvalidParameterOnComponent(node.Source, node.AttributeName, _component.TagName));
                    return;
                }

                _children.Add(new ComponentAttributeExtensionNode(node));
            }

            public override void VisitDefault(IntermediateNode node)
            {
                _children.Add(node);
            }
        }

        private class ElementRewriteVisitor : IntermediateNodeWalker
        {
            private readonly IntermediateNodeCollection _children;

            public ElementRewriteVisitor(IntermediateNodeCollection children)
            {
                _children = children;
            }

            public override void VisitTagHelper(TagHelperIntermediateNode node)
            {
                // Visit children, we're replacing this node.
                for (var i = 0; i < node.Children.Count; i++)
                {
                    Visit(node.Children[i]);
                }
            }

            public override void VisitTagHelperBody(TagHelperBodyIntermediateNode node)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    _children.Add(node.Children[i]);
                }
            }

            public override void VisitTagHelperHtmlAttribute(TagHelperHtmlAttributeIntermediateNode node)
            {
                var attribute = new HtmlAttributeIntermediateNode()
                {
                    AttributeName = node.AttributeName,
                    Source = node.Source,
                };
                _children.Add(attribute);

                for (var i = 0; i < node.Diagnostics.Count; i++)
                {
                    attribute.Diagnostics.Add(node.Diagnostics[i]);
                }

                switch (node.AttributeStructure)
                {
                    case AttributeStructure.Minimized:

                        attribute.Prefix = node.AttributeName;
                        attribute.Suffix = string.Empty;
                        break;

                    case AttributeStructure.NoQuotes:
                    case AttributeStructure.SingleQuotes:
                    case AttributeStructure.DoubleQuotes:

                        // We're ignoring attribute structure here for simplicity, it doesn't effect us.
                        attribute.Prefix = node.AttributeName + "=\"";
                        attribute.Suffix = "\"";

                        for (var i = 0; i < node.Children.Count; i++)
                        {
                            attribute.Children.Add(RewriteAttributeContent(node.Children[i]));
                        }

                        break;
                }

                IntermediateNode RewriteAttributeContent(IntermediateNode content)
                {
                    if (content is HtmlContentIntermediateNode html)
                    {
                        var value = new HtmlAttributeValueIntermediateNode()
                        {
                            Source = content.Source,
                        };

                        for (var i = 0; i < html.Children.Count; i++)
                        {
                            value.Children.Add(html.Children[i]);
                        }

                        for (var i = 0; i < html.Diagnostics.Count; i++)
                        {
                            value.Diagnostics.Add(html.Diagnostics[i]);
                        }

                        return value;
                    }


                    return content;
                }
            }

            public override void VisitTagHelperProperty(TagHelperPropertyIntermediateNode node)
            {
                // Each 'tag helper property' belongs to a specific tag helper. We want to handle
                // the cases for components, but leave others alone. This allows our other passes
                // to handle those cases.
                _children.Add(node.TagHelper.IsComponentTagHelper() ? (IntermediateNode)new ComponentAttributeExtensionNode(node) : node);
            }

            public override void VisitDefault(IntermediateNode node)
            {
                _children.Add(node);
            }
        }
    }
}
