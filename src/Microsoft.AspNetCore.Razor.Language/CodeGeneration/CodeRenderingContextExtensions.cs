// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public static class CodeRenderingContextExtensions
    {
        private static readonly object RenderNodeKey = new object();
        private static readonly object RenderChildrenKey = new object();
        private static readonly object LineMappingsKey = new object();

        public static void SetRenderNode(this CodeRenderingContext context, Action<IntermediateNode> renderNode)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.Items[RenderNodeKey] = renderNode;
        }

        public static void RenderNode(this CodeRenderingContext context, IntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var renderNode = (Action<IntermediateNode>)context.Items[RenderNodeKey];

            if (renderNode == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatRenderingContextRequiresDelegate(nameof(CodeRenderingContext), nameof(RenderNode)));
            }

            renderNode(node);
        }

        public static void SetRenderChildren(this CodeRenderingContext context, Action<IntermediateNode> renderChildren)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            context.Items[RenderChildrenKey] = renderChildren;
        }

        public static void RenderChildren(this CodeRenderingContext context, IntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var renderChildren = (Action<IntermediateNode>)context.Items[RenderChildrenKey];
            if (renderChildren == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatRenderingContextRequiresDelegate(nameof(CodeRenderingContext), nameof(RenderChildren)));
            }

            renderChildren(node);
        }

        public static void AddLineMappingFor(this CodeRenderingContext context, IntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var lineMappings = (List<LineMapping>)context.Items[LineMappingsKey];
            if (lineMappings == null)
            {
                lineMappings = new List<LineMapping>();
                context.Items[LineMappingsKey] = lineMappings;
            }

            if (node.Source == null)
            {
                return;
            }

            if (context.SourceDocument.FilePath != null &&
                !string.Equals(context.SourceDocument.FilePath, node.Source.Value.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                // We don't want to generate line mappings for imports.
                return;
            }

            var source = node.Source.Value;
            var generatedLocation = new SourceSpan(context.CodeWriter.Location, source.Length);
            var lineMapping = new LineMapping(source, generatedLocation);

            lineMappings.Add(lineMapping);
        }

        public static IReadOnlyList<LineMapping> GetLineMappings(this CodeRenderingContext context)
        {
            var lineMappings = (IReadOnlyList<LineMapping>)context.Items[LineMappingsKey] ?? Array.Empty<LineMapping>();

            return lineMappings;
        }
    }
}
