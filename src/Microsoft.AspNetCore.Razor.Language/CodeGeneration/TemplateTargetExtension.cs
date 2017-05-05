// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class TemplateTargetExtension : ITemplateTargetExtension
    {
        public static readonly string DefaultTemplateTypeName = "Microsoft.AspNetCore.Mvc.Razor.HelperResult";
        public static readonly string DefaultPushWriterMethod = "PushWriter";
        public static readonly string DefaultPopWriterMethod = "PopWriter";

        public string TemplateTypeName { get; set; } = DefaultTemplateTypeName;

        public string PushWriterMethod { get; set; } = DefaultPushWriterMethod;

        public string PopWriterMethod { get; set; } = DefaultPopWriterMethod;

        public void WriteTemplate(CSharpRenderingContext context, TemplateIRNode node)
        {
            const string ItemParameterName = "item";
            const string TemplateWriterName = "__razor_template_writer";

            context.Writer
                .Write(ItemParameterName).Write(" => ")
                .WriteStartNewObject(TemplateTypeName);

            using (context.Writer.BuildAsyncLambda(endLine: false, parameterNames: TemplateWriterName))
            {
                if (!context.Options.DesignTimeMode)
                {
                    context.Writer.WriteMethodInvocation(PushWriterMethod, TemplateWriterName);
                }

                context.RenderChildren(node);

                if (!context.Options.DesignTimeMode)
                {
                    context.Writer.WriteMethodInvocation(PopWriterMethod);
                }
            }

            context.Writer.WriteEndMethodInvocation(endLine: false);
        }
    }
}
