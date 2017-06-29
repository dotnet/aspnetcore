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
    public class RuntimeNodeWriter : IntermediateNodeWriter
    {
        public virtual string WriteCSharpExpressionMethod { get; set; } = "Write";

        public virtual string WriteHtmlContentMethod { get; set; } = "WriteLiteral";

        public virtual string BeginWriteAttributeMethod { get; set; } = "BeginWriteAttribute";

        public virtual string EndWriteAttributeMethod { get; set; } = "EndWriteAttribute";

        public virtual string WriteAttributeValueMethod { get; set; } = "WriteAttributeValue";

        public virtual string PushWriterMethod { get; set; } = "PushWriter";

        public virtual string PopWriterMethod { get; set; } = "PopWriter";

        public string TemplateTypeName { get; set; } = "Microsoft.AspNetCore.Mvc.Razor.HelperResult";

        public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
        {
            if (node.Source.HasValue)
            {
                using (context.CodeWriter.BuildLinePragma(node.Source.Value))
                {
                    context.CodeWriter.WriteUsing(node.Content);
                }
            }
            else
            {
                context.CodeWriter.WriteUsing(node.Content);
            }
        }

        public override void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            IDisposable linePragmaScope = null;
            if (node.Source != null)
            {
                linePragmaScope = context.CodeWriter.BuildLinePragma(node.Source.Value);
                context.CodeWriter.WritePadding(WriteCSharpExpressionMethod.Length + 1, node.Source, context);
            }

            context.CodeWriter.WriteStartMethodInvocation(WriteCSharpExpressionMethod);

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                {
                    context.CodeWriter.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the expression like a Template or another extension node.
                    context.RenderNode(node.Children[i]);
                }
            }

            context.CodeWriter.WriteEndMethodInvocation();

            linePragmaScope?.Dispose();
        }

        public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node)
        {
            var isWhitespaceStatement = true;
            for (var i = 0; i < node.Children.Count; i++)
            {
                var token = node.Children[i] as IntermediateToken;
                if (token == null || !string.IsNullOrWhiteSpace(token.Content))
                {
                    isWhitespaceStatement = false;
                    break;
                }
            }

            if (isWhitespaceStatement)
            {
                return;
            }

            IDisposable linePragmaScope = null;
            if (node.Source != null)
            {
                linePragmaScope = context.CodeWriter.BuildLinePragma(node.Source.Value);
                context.CodeWriter.WritePadding(0, node.Source.Value, context);
            }

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                {
                    context.CodeWriter.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the statement like an extension node.
                    context.RenderNode(node.Children[i]);
                }
            }

            if (linePragmaScope == null)
            {
                context.CodeWriter.WriteLine();
            }

            linePragmaScope?.Dispose();
        }

        public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
        {
            var valuePieceCount = node
                .Children
                .Count(child =>
                    child is HtmlAttributeValueIntermediateNode ||
                    child is CSharpExpressionAttributeValueIntermediateNode ||
                    child is CSharpCodeAttributeValueIntermediateNode ||
                    child is ExtensionIntermediateNode);

            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var suffixLocation = node.Source.Value.AbsoluteIndex + node.Source.Value.Length - node.Suffix.Length;
            context.CodeWriter
                .WriteStartMethodInvocation(BeginWriteAttributeMethod)
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

            context.CodeWriter
                .WriteStartMethodInvocation(EndWriteAttributeMethod)
                .WriteEndMethodInvocation();
        }

        public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node)
        {
            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
            var valueLength = node.Source.Value.Length;
            context.CodeWriter
                .WriteStartMethodInvocation(WriteAttributeValueMethod)
                .WriteStringLiteral(node.Prefix)
                .WriteParameterSeparator()
                .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator();

            // Write content
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsHtml)
                {
                    context.CodeWriter.WriteStringLiteral(token.Content);
                }
                else
                {
                    // There may be something else inside the attribute value like an extension node.
                    context.RenderNode(node.Children[i]);
                }
            }

            context.CodeWriter
                .WriteParameterSeparator()
                .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .WriteBooleanLiteral(true)
                .WriteEndMethodInvocation();
        }

        public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node)
        {
            using (context.CodeWriter.BuildLinePragma(node.Source.Value))
            {
                var prefixLocation = node.Source.Value.AbsoluteIndex;
                context.CodeWriter
                    .WriteStartMethodInvocation(WriteAttributeValueMethod)
                    .WriteStringLiteral(node.Prefix)
                    .WriteParameterSeparator()
                    .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator();

                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                    {
                        context.CodeWriter.Write(token.Content);
                    }
                    else
                    {
                        // There may be something else inside the expression like an extension node.
                        context.RenderNode(node.Children[i]);
                    }
                }

                var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
                var valueLength = node.Source.Value.Length - node.Prefix.Length;
                context.CodeWriter
                    .WriteParameterSeparator()
                    .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .WriteBooleanLiteral(false)
                    .WriteEndMethodInvocation();
            }
        }

        public override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node)
        {
            const string ValueWriterName = "__razor_attribute_value_writer";

            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
            var valueLength = node.Source.Value.Length - node.Prefix.Length;
            context.CodeWriter
                .WriteStartMethodInvocation(WriteAttributeValueMethod)
                .WriteStringLiteral(node.Prefix)
                .WriteParameterSeparator()
                .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator();

            context.CodeWriter.WriteStartNewObject(TemplateTypeName);

            using (context.CodeWriter.BuildAsyncLambda(ValueWriterName))
            {
                BeginWriterScope(context, ValueWriterName);

                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                    {
                        var isWhitespaceStatement = string.IsNullOrWhiteSpace(token.Content);
                        IDisposable linePragmaScope = null;
                        if (token.Source != null)
                        {
                            if (!isWhitespaceStatement)
                            {
                                linePragmaScope = context.CodeWriter.BuildLinePragma(token.Source.Value);
                            }

                            context.CodeWriter.WritePadding(0, token.Source.Value, context);
                        }
                        else if (isWhitespaceStatement)
                        {
                            // Don't write whitespace if there is no line mapping for it.
                            continue;
                        }

                        context.CodeWriter.Write(token.Content);

                        if (linePragmaScope != null)
                        {
                            linePragmaScope.Dispose();
                        }
                        else
                        {
                            context.CodeWriter.WriteLine();
                        }
                    }
                    else
                    {
                        // There may be something else inside the statement like an extension node.
                        context.RenderNode(node.Children[i]);
                    }
                }

                EndWriterScope(context);
            }

            context.CodeWriter.WriteEndMethodInvocation(false);

            context.CodeWriter
                .WriteParameterSeparator()
                .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .WriteBooleanLiteral(false)
                .WriteEndMethodInvocation();
        }

        public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node)
        {
            const int MaxStringLiteralLength = 1024;

            var builder = new StringBuilder();
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsHtml)
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

                context.CodeWriter
                    .WriteStartMethodInvocation(WriteHtmlContentMethod)
                    .WriteStringLiteral(textToRender)
                    .WriteEndMethodInvocation();

                charactersConsumed += textToRender.Length;
            }
        }

        public override void BeginWriterScope(CodeRenderingContext context, string writer)
        {
            context.CodeWriter.WriteMethodInvocation(PushWriterMethod, writer);
        }

        public override void EndWriterScope(CodeRenderingContext context)
        {
            context.CodeWriter.WriteMethodInvocation(PopWriterMethod);
        }
    }
}
