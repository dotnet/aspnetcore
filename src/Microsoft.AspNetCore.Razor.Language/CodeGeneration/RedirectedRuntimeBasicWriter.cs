// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class RedirectedRuntimeBasicWriter : RuntimeBasicWriter
    {
        private readonly string _textWriter;

        public RedirectedRuntimeBasicWriter(string textWriter)
        {
            _textWriter = textWriter;
        }

        public override string WriteCSharpExpressionMethod { get; set; } = "WriteTo";

        public override string WriteHtmlContentMethod { get; set; } = "WriteLiteralTo";

        public override string BeginWriteAttributeMethod { get; set; } = "BeginWriteAttributeTo";

        public override string EndWriteAttributeMethod { get; set; } = "EndWriteAttributeTo";

        public override string WriteAttributeValueMethod { get; set; } = "WriteAttributeValueTo";

        public override void WriteCSharpExpression(CSharpRenderingContext context, CSharpExpressionIRNode node)
        {
            IDisposable linePragmaScope = null;
            if (node.Source != null)
            {
                linePragmaScope = context.Writer.BuildLinePragma(node.Source.Value);

                var offset = WriteCSharpExpressionMethod.Length + "(".Length + _textWriter.Length + ", ".Length;
                context.Writer.WritePadding(offset, node.Source, context);
            }

            context.Writer.WriteStartMethodInvocation(WriteCSharpExpressionMethod);
            context.Writer.Write(_textWriter);
            context.Writer.WriteParameterSeparator();

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is RazorIRToken token && token.IsCSharp)
                {
                    context.Writer.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the expression like a Template or another extension node.
                    context.RenderNode(node.Children[i]);
                }
            }

            context.Writer.WriteEndMethodInvocation();

            linePragmaScope?.Dispose();
        }

        public override void WriteHtmlContent(CSharpRenderingContext context, HtmlContentIRNode node)
        {
            const int MaxStringLiteralLength = 1024;

            var builder = new StringBuilder();
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is RazorIRToken token && token.IsHtml)
                {
                    builder.Append(token.Content);
                }
            }

            var content = builder.ToString();

            var charactersConsumed = 0;

            // Render the string in pieces to avoid Roslyn OOM exceptions at compile time: https://github.com/aspnet/External/issues/54
            while (charactersConsumed < content.Length)
            {
                string textToRender;
                if (content.Length <= MaxStringLiteralLength)
                {
                    textToRender = content;
                }
                else
                {
                    var charactersToSubstring = Math.Min(MaxStringLiteralLength, content.Length - charactersConsumed);
                    textToRender = content.Substring(charactersConsumed, charactersToSubstring);
                }

                context.Writer
                    .WriteStartMethodInvocation(WriteHtmlContentMethod)
                    .Write(_textWriter)
                    .WriteParameterSeparator()
                    .WriteStringLiteral(textToRender)
                    .WriteEndMethodInvocation();

                    charactersConsumed += textToRender.Length;
            }
        }

        public override void WriteHtmlAttribute(CSharpRenderingContext context, HtmlAttributeIRNode node)
        {
            var valuePieceCount = node
                .Children
                .Count(child => child is HtmlAttributeValueIRNode || child is CSharpAttributeValueIRNode);
            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var suffixLocation = node.Source.Value.AbsoluteIndex + node.Source.Value.Length - node.Suffix.Length;
            context.Writer
                .WriteStartMethodInvocation(BeginWriteAttributeMethod)
                .Write(_textWriter)
                .WriteParameterSeparator()
                .WriteStringLiteral(node.Name)
                .WriteParameterSeparator()
                .WriteStringLiteral(node.Prefix)
                .WriteParameterSeparator()
                .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .WriteStringLiteral(node.Suffix)
                .WriteParameterSeparator()
                .Write(suffixLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .Write(valuePieceCount.ToString(CultureInfo.InvariantCulture))
                .WriteEndMethodInvocation();

            context.RenderChildren(node);

            context.Writer
                .WriteStartMethodInvocation(EndWriteAttributeMethod)
                .Write(_textWriter)
                .WriteEndMethodInvocation();
        }

        public override void WriteHtmlAttributeValue(CSharpRenderingContext context, HtmlAttributeValueIRNode node)
        {
            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
            var valueLength = node.Source.Value.Length;
            context.Writer
                .WriteStartMethodInvocation(WriteAttributeValueMethod)
                .Write(_textWriter)
                .WriteParameterSeparator()
                .WriteStringLiteral(node.Prefix)
                .WriteParameterSeparator()
                .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .WriteStringLiteral(node.Content)
                .WriteParameterSeparator()
                .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .WriteBooleanLiteral(true)
                .WriteEndMethodInvocation();
        }

        public override void WriteCSharpAttributeValue(CSharpRenderingContext context, CSharpAttributeValueIRNode node)
        {
            const string ValueWriterName = "__razor_attribute_value_writer";

            var expressionValue = node.Children.FirstOrDefault() as CSharpExpressionIRNode;
            var linePragma = expressionValue != null ? context.Writer.BuildLinePragma(node.Source.Value) : null;
            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
            var valueLength = node.Source.Value.Length - node.Prefix.Length;
            context.Writer
                .WriteStartMethodInvocation(WriteAttributeValueMethod)
                .Write(_textWriter)
                .WriteParameterSeparator()
                .WriteStringLiteral(node.Prefix)
                .WriteParameterSeparator()
                .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator();

            if (expressionValue != null)
            {
                Debug.Assert(node.Children.Count == 1);

                RenderExpressionInline(context, expressionValue);
            }
            else
            {
                // Not an expression; need to buffer the result.
                context.Writer.WriteStartNewObject(TemplateTypeName);

                using (context.Push(new RedirectedRuntimeBasicWriter(ValueWriterName)))
                using (context.Writer.BuildAsyncLambda(endLine: false, parameterNames: ValueWriterName))
                {
                    context.RenderChildren(node);
                }

                context.Writer.WriteEndMethodInvocation(false);
            }

            context.Writer
                .WriteParameterSeparator()
                .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .WriteBooleanLiteral(false)
                .WriteEndMethodInvocation();

            linePragma?.Dispose();
        }
    }
}
