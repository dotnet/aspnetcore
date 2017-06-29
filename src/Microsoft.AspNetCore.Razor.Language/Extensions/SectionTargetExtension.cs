// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public sealed class SectionTargetExtension : ISectionTargetExtension
    {
        private static readonly string DefaultWriterName = "__razor_section_writer";

        public static readonly string DefaultSectionMethodName = "DefineSection";

        public string SectionMethodName { get; set; } = DefaultSectionMethodName;

        public void WriteSection(CodeRenderingContext context, SectionIntermediateNode node)
        {
            // Quirk Alert!
            //
            // In 1.0.0 Razor/MVC the define section method took a parameter for a TextWriter
            // that would be used for all of the output in the section. We simplified this API for
            // 2.0.0 of MVC, but our design time codegen still needs to target 1.0.0.
            //
            // So the workaround is MVC 2.0.0 will define these methods with the TextWriter, but
            // that method is never called. We still generate the call *with* the TextWriter for 
            // design time, at least until we have multi-targeting.
            var writerName = context.Options.DesignTime ? DefaultWriterName : string.Empty;

            context.CodeWriter
                .WriteStartMethodInvocation(SectionMethodName)
                .Write("\"")
                .Write(node.Name)
                .Write("\", ");

            using (context.CodeWriter.BuildAsyncLambda(writerName))
            {
                context.RenderChildren(node);
            }

            context.CodeWriter.WriteEndMethodInvocation(endLine: true);
        }
    }
}
