// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal class DefaultTagHelperOptimizationPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run later than default order for user code so other passes have a chance to modify the
        // tag helper nodes.
        public override int Order => DefaultFeatureOrder + 1000;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @class = documentNode.FindPrimaryClass();
            if (@class == null)
            {
                // Bail if we can't find a class node, we need to be able to create fields.
                return;
            }

            var context = new Context(@class);

            // First find all tag helper nodes that require the default tag helper runtime.
            //
            // This phase lowers the conceptual nodes to default runtime nodes we only care about those.
            var tagHelperNodes = documentNode
                .FindDescendantNodes<TagHelperIntermediateNode>()
                .Where(IsTagHelperRuntimeNode)
                .ToArray();

            if (tagHelperNodes.Length == 0)
            {
                // If nothing uses the default runtime then we're done.
                return;
            }

            AddDefaultRuntime(context);

            // Each tagHelperNode should be rewritten to use the default tag helper runtime. That doesn't necessarily
            // mean that all of these tag helpers are the default kind, just that them are compatible with ITagHelper.
            for (var i = 0; i < tagHelperNodes.Length; i++)
            {
                var tagHelperNode = tagHelperNodes[i];

                RewriteBody(tagHelperNode);
                RewriteHtmlAttributes(tagHelperNode);
                AddExecute(tagHelperNode);

                // We need to find all of the 'default' kind tag helpers and rewrite their usage site to use the 
                // extension nodes for the default tag helper runtime (ITagHelper).
                foreach (var tagHelper in tagHelperNode.TagHelpers)
                {
                    RewriteUsage(context, tagHelperNode, tagHelper);
                }
            }

            // Then for each 'default' kind tag helper we need to generate the field that will hold it.
            foreach (var tagHelper in context.TagHelpers)
            {
                AddField(context, tagHelper);
            }
        }

        private void AddDefaultRuntime(Context context)
        {
            // We need to insert a node for the field that will hold the tag helper. We've already generated a field name
            // at this time and use it for all uses of the same tag helper type.
            //
            // We also want to preserve the ordering of the nodes for testability. So insert at the end of any existing
            // field nodes.
            var i = 0;
            while (i < context.Class.Children.Count && context.Class.Children[i] is FieldDeclarationIntermediateNode)
            {
                i++;
            }

            context.Class.Children.Insert(i, new DefaultTagHelperRuntimeIntermediateNode());
        }

        private void RewriteBody(TagHelperIntermediateNode node)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is TagHelperBodyIntermediateNode bodyNode)
                {
                    // We only expect one body node.
                    node.Children[i] = new DefaultTagHelperBodyIntermediateNode(bodyNode)
                    {
                        TagMode = node.TagMode,
                        TagName = node.TagName,
                    };
                    break;
                }
            }
        }

        private void AddExecute(TagHelperIntermediateNode node)
        {
            // Execute the tag helpers at the end, before leaving scope.
            node.Children.Add(new DefaultTagHelperExecuteIntermediateNode());
        }

        private void RewriteHtmlAttributes(TagHelperIntermediateNode node)
        {
            // We need to rewrite each html attribute, so that it will get added to the execution context.
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is TagHelperHtmlAttributeIntermediateNode htmlAttributeNode)
                {
                    node.Children[i] = new DefaultTagHelperHtmlAttributeIntermediateNode(htmlAttributeNode);
                }
            }
        }

        private void RewriteUsage(Context context, TagHelperIntermediateNode node, TagHelperDescriptor tagHelper)
        {
            if (!tagHelper.IsDefaultKind())
            {
                return;
            }

            context.Add(tagHelper);

            // First we need to insert a node for the creation of the tag helper, and the hook up to the execution
            // context. This should come after the body node and any existing create nodes.
            //
            // If we're dealing with something totally malformed, then we'll end up just inserting at the end, and that's not
            // so bad.
            var i = 0;

            // Find the body node.
            while (i < node.Children.Count && node.Children[i] is TagHelperBodyIntermediateNode)
            {
                i++;
            }
            while (i < node.Children.Count && node.Children[i] is DefaultTagHelperBodyIntermediateNode)
            {
                i++;
            }

            // Now find the last create node.
            while (i < node.Children.Count && node.Children[i] is DefaultTagHelperCreateIntermediateNode)
            {
                i++;
            }

            // Now i has the right insertion point.
            node.Children.Insert(i, new DefaultTagHelperCreateIntermediateNode()
            {
                Field = context.GetFieldName(tagHelper),
                TagHelper = tagHelper,
                Type = tagHelper.GetTypeName(),
            });

            // Next we need to rewrite any property nodes to use the field and property name for this
            // tag helper.
            for (i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is TagHelperPropertyIntermediateNode propertyNode &&
                    propertyNode.TagHelper == tagHelper)
                {
                    // This belongs to the current tag helper, replace it.
                    node.Children[i] = new DefaultTagHelperPropertyIntermediateNode(propertyNode)
                    {
                        Field = context.GetFieldName(tagHelper),
                        Property = propertyNode.BoundAttribute.GetPropertyName(),
                    };
                }
            }
        }

        private void AddField(Context context, TagHelperDescriptor tagHelper)
        {
            // We need to insert a node for the field that will hold the tag helper. We've already generated a field name
            // at this time and use it for all uses of the same tag helper type.
            //
            // We also want to preserve the ordering of the nodes for testability. So insert at the end of any existing
            // field nodes.
            var i = 0;
            while (i < context.Class.Children.Count && context.Class.Children[i] is DefaultTagHelperRuntimeIntermediateNode)
            {
                i++;
            }

            while (i < context.Class.Children.Count && context.Class.Children[i] is FieldDeclarationIntermediateNode)
            {
                i++;
            }

            context.Class.Children.Insert(i, new FieldDeclarationIntermediateNode()
            {
                Annotations =
                {
                    { CommonAnnotations.DefaultTagHelperExtension.TagHelperField, bool.TrueString },
                },
                Modifiers =
                {
                    "private",
                },
                FieldName = context.GetFieldName(tagHelper),
                FieldType = "global::" + tagHelper.GetTypeName(),
            });
        }

        private bool IsTagHelperRuntimeNode(TagHelperIntermediateNode node)
        {
            foreach (var tagHelper in node.TagHelpers)
            {
                if (tagHelper.KindUsesDefaultTagHelperRuntime())
                {
                    return true;
                }
            }

            return false;
        }

        private struct Context
        {
            private readonly Dictionary<TagHelperDescriptor, string> _tagHelpers;

            public Context(ClassDeclarationIntermediateNode @class)
            {
                Class = @class;

                _tagHelpers = new Dictionary<TagHelperDescriptor, string>(TagHelperDescriptorComparer.Default);
            }

            public ClassDeclarationIntermediateNode Class { get; }

            public IEnumerable<TagHelperDescriptor> TagHelpers => _tagHelpers.Keys;

            public bool Add(TagHelperDescriptor tagHelper)
            {
                if (_tagHelpers.ContainsKey(tagHelper))
                {
                    return false;
                }

                _tagHelpers.Add(tagHelper, GenerateFieldName(tagHelper));
                return true;
            }

            public string GetFieldName(TagHelperDescriptor tagHelper)
            {
                return _tagHelpers[tagHelper];
            }

            private static string GenerateFieldName(TagHelperDescriptor tagHelper)
            {
                return "__" + tagHelper.GetTypeName().Replace('.', '_');
            }
        }
    }
}
