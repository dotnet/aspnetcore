// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.Blazor.Build.Core.RazorCompilation.Engine
{
    /// <summary>
    /// Generates the C# code corresponding to Razor source document contents.
    /// </summary>
    internal class BlazorIntermediateNodeWriter : IntermediateNodeWriter
    {
        public override void BeginWriterScope(CodeRenderingContext context, string writer)
        {
            throw new System.NotImplementedException(nameof(BeginWriterScope));
        }

        public override void EndWriterScope(CodeRenderingContext context)
        {
            throw new System.NotImplementedException(nameof(EndWriterScope));
        }

        public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node)
        {
            throw new System.NotImplementedException(nameof(WriteCSharpCode));
        }

        public override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node)
        {
            throw new System.NotImplementedException(nameof(WriteCSharpCodeAttributeValue));
        }

        public override void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node)
        {
            throw new System.NotImplementedException(nameof(WriteCSharpExpression));
        }

        public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node)
        {
            throw new System.NotImplementedException(nameof(WriteCSharpExpressionAttributeValue));
        }

        public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
        {
            throw new System.NotImplementedException(nameof(WriteHtmlAttribute));
        }

        public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node)
        {
            throw new System.NotImplementedException(nameof(WriteHtmlAttributeValue));
        }

        public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node)
        {
            context.CodeWriter.Write("/* HTML content */");
        }

        public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
        {
            throw new System.NotImplementedException(nameof(WriteUsingDirective));
        }
    }
}
