// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
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

            var initialRenderingConventions = context.RenderingConventions;
            context.RenderingConventions = new CSharpRedirectRenderingConventions(TemplateWriterName, context.Writer);
            using (context.Writer.BuildAsyncLambda(endLine: false, parameterNames: TemplateWriterName))
            {
                context.RenderChildren(node);
            }
            context.RenderingConventions = initialRenderingConventions;

            context.Writer.WriteEndMethodInvocation(endLine: false);
        }
    }
}
