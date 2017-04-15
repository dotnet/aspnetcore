// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class TemplateTargetExtension : ITemplateTargetExtension
    {
        public static readonly string DefaultTemplateTypeName = "Microsoft.AspNetCore.Mvc.Razor.HelperResult";

        public string TemplateTypeName { get; set; } = DefaultTemplateTypeName;

        public void WriteTemplate(CSharpRenderingContext context, TemplateIRNode node)
        {
            const string ItemParameterName = "item";
            const string TemplateWriterName = "__razor_template_writer";

            context.Writer
                .Write(ItemParameterName).Write(" => ")
                .WriteStartNewObject(TemplateTypeName);

            IDisposable basicWriterScope = null;
            IDisposable tagHelperWriterScope = null;
            if (!context.Options.DesignTimeMode)
            {
                basicWriterScope = context.Push(new RedirectedRuntimeBasicWriter(TemplateWriterName));
                tagHelperWriterScope = context.Push(new RedirectedRuntimeTagHelperWriter(TemplateWriterName));
            }

            using (context.Writer.BuildAsyncLambda(endLine: false, parameterNames: TemplateWriterName))
            {
                context.RenderChildren(node);
            }

            basicWriterScope?.Dispose();
            tagHelperWriterScope?.Dispose();

            context.Writer.WriteEndMethodInvocation(endLine: false);
        }
    }
}
