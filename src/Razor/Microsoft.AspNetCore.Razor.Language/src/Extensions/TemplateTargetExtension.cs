// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public sealed class TemplateTargetExtension : ITemplateTargetExtension
{
    public static readonly string DefaultTemplateTypeName = "Template";

    public string TemplateTypeName { get; set; } = DefaultTemplateTypeName;

    public void WriteTemplate(CodeRenderingContext context, TemplateIntermediateNode node)
    {
        const string ItemParameterName = "item";
        const string TemplateWriterName = "__razor_template_writer";

        context.CodeWriter
            .Write(ItemParameterName).Write(" => ")
            .WriteStartNewObject(TemplateTypeName);

        using (context.CodeWriter.BuildAsyncLambda(TemplateWriterName))
        {
            context.NodeWriter.BeginWriterScope(context, TemplateWriterName);

            context.RenderChildren(node);

            context.NodeWriter.EndWriterScope(context);
        }

        context.CodeWriter.WriteEndMethodInvocation(endLine: false);
    }
}
