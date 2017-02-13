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

        public override void VisitNamespace(NamespaceDeclarationIRNode node)
        {
            Context.Writer
                .Write("namespace ")
                .WriteLine(node.Content);

            using (Context.Writer.BuildScope())
            {
                Context.Writer.WriteLineHiddenDirective();
                VisitDefault(node);
            }
        }

        public override void VisitRazorMethodDeclaration(RazorMethodDeclarationIRNode node)
        {
            Context.Writer.WriteLine("#pragma warning disable 1998");

            Context.Writer
                .Write(node.AccessModifier)
                .Write(" ");

            if (node.Modifiers != null)
            {
                for (var i = 0; i < node.Modifiers.Count; i++)
                {
                    Context.Writer.Write(node.Modifiers[i]);

                    if (i + 1 < node.Modifiers.Count)
                    {
                        Context.Writer.Write(" ");
                    }
                }
            }

            Context.Writer
                .Write(" ")
                .Write(node.ReturnType)
                .Write(" ")
                .Write(node.Name)
                .WriteLine("()");

            using (Context.Writer.BuildScope())
            {
                VisitDefault(node);
            }

            Context.Writer.WriteLine("#pragma warning restore 1998");
        }

        public override void VisitClass(ClassDeclarationIRNode node)
        {
            Context.Writer
                .Write(node.AccessModifier)
                .Write(" class ")
                .Write(node.Name);

            if (node.BaseType != null || node.Interfaces != null)
            {
                Context.Writer.Write(" : ");
            }

            if (node.BaseType != null)
            {
                Context.Writer.Write(node.BaseType);

                if (node.Interfaces != null)
                {
                    Context.Writer.WriteParameterSeparator();
                }
            }

            if (node.Interfaces != null)
            {
                for (var i = 0; i < node.Interfaces.Count; i++)
                {
                    Context.Writer.Write(node.Interfaces[i]);

                    if (i + 1 < node.Interfaces.Count)
                    {
                        Context.Writer.WriteParameterSeparator();
                    }
                }
            }

            Context.Writer.WriteLine();

            using (Context.Writer.BuildScope())
            {
                VisitDefault(node);
            }
        }

        public override void VisitExtension(ExtensionIRNode node)
        {
            node.WriteNode(Target, Context);
        }

        protected static void RenderExpressionInline(RazorIRNode node, CSharpRenderingContext context)
        {
            if (node is CSharpTokenIRNode)
            {
                context.Writer.Write(((CSharpTokenIRNode)node).Content);
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
            string tagHelperVariableName,
            string attributeName,
            TagHelperAttributeDescriptor descriptor)
        {
            var propertyAccessor = $"{tagHelperVariableName}.{descriptor.PropertyName}";

            if (descriptor.IsIndexer)
            {
                var dictionaryKey = attributeName.Substring(descriptor.Name.Length);
                propertyAccessor += $"[\"{dictionaryKey}\"]";
            }

            return propertyAccessor;
        }
    }
}
