// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class PageStructureCSharpRenderer : RazorIRNodeWalker
    {
        protected readonly CSharpRenderingContext Context;
        protected readonly RuntimeTarget Target;

        public PageStructureCSharpRenderer(RuntimeTarget target, CSharpRenderingContext context)
        {
            Context = context;
            Target = target;
        }

        public override void VisitExtension(ExtensionIRNode node)
        {
            // This needs to stay here until the rest of the code in the renderers is rewritten because
            // and extension can occur at any level.
            node.WriteNode(Target, Context);
        }

        protected static void RenderExpressionInline(RazorIRNode node, CSharpRenderingContext context)
        {
            if (node is RazorIRToken token && token.IsCSharp)
            {
                context.Writer.Write(token.Content);
            }
            else
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    RenderExpressionInline(node.Children[i], context);
                }
            }
        }

        protected static int CalculateExpressionPadding(SourceSpan sourceRange, CSharpRenderingContext context)
        {
            var spaceCount = 0;
            for (var i = sourceRange.AbsoluteIndex - 1; i >= 0; i--)
            {
                var @char = context.SourceDocument[i];
                if (@char == '\n' || @char == '\r')
                {
                    break;
                }
                else if (@char == '\t')
                {
                    spaceCount += context.Options.TabSize;
                }
                else
                {
                    spaceCount++;
                }
            }

            return spaceCount;
        }

        protected static string BuildOffsetPadding(int generatedOffset, SourceSpan sourceRange, CSharpRenderingContext context)
        {
            var basePadding = CalculateExpressionPadding(sourceRange, context);
            var resolvedPadding = Math.Max(basePadding - generatedOffset, 0);

            if (context.Options.IsIndentingWithTabs)
            {
                var spaces = resolvedPadding % context.Options.TabSize;
                var tabs = resolvedPadding / context.Options.TabSize;

                return new string('\t', tabs) + new string(' ', spaces);
            }
            else
            {
                return new string(' ', resolvedPadding);
            }
        }

        protected static string GetTagHelperVariableName(string tagHelperTypeName) => "__" + tagHelperTypeName.Replace('.', '_');

        protected static string GetTagHelperPropertyAccessor(
            bool isIndexerNameMatch,
            string tagHelperVariableName,
            string attributeName,
            BoundAttributeDescriptor descriptor)
        {
            var propertyAccessor = $"{tagHelperVariableName}.{descriptor.Metadata[ITagHelperBoundAttributeDescriptorBuilder.PropertyNameKey]}";

            if (isIndexerNameMatch)
            {
                var dictionaryKey = attributeName.Substring(descriptor.IndexerNamePrefix.Length);
                propertyAccessor += $"[\"{dictionaryKey}\"]";
            }

            return propertyAccessor;
        }
    }
}
