// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public sealed class SectionTargetExtension : ISectionTargetExtension
    {
        // Compatibility for 1.X projects
        private const string DefaultWriterName = "__razor_section_writer";

        public static readonly string DefaultSectionMethodName = "DefineSection";

        public string SectionMethodName { get; set; } = DefaultSectionMethodName;

        public void WriteSection(CodeRenderingContext context, SectionIntermediateNode node)
        {
            context.CodeWriter
                .WriteStartMethodInvocation(SectionMethodName)
                .Write("\"")
                .Write(node.SectionName)
                .Write("\", ");

            if (context.Options.DesignTime)
            {
                using (context.CodeWriter.BuildAsyncLambda(DefaultWriterName))
                {
                    context.RenderChildren(node);
                }
            }
            else
            {
                using (context.CodeWriter.BuildAsyncLambda())
                {
                    context.RenderChildren(node);
                }
            }

            context.CodeWriter.WriteEndMethodInvocation(endLine: true);
        }
    }
}
