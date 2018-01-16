// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Parser.Html;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.Blazor.RenderTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Blazor.Build.Core.RazorCompilation.Engine
{
    /// <summary>
    /// Generates the C# code corresponding to Razor source document contents.
    /// </summary>
    internal class BlazorIntermediateNodeWriter : IntermediateNodeWriter
    {
        private const string builderVarName = "builder";

        // Per the HTML spec, the following elements are inherently self-closing
        // For example, <img> is the same as <img /> (and therefore it cannot contain descendants)
        private static HashSet<string> htmlVoidElementsLookup
            = new HashSet<string>(
                new[] { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" },
                StringComparer.OrdinalIgnoreCase);

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
        }

        public override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node)
        {
            throw new System.NotImplementedException(nameof(WriteCSharpCodeAttributeValue));
        }

        public override void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node)
        {
            // Render as text node. Later, we'll need to add different handling for expressions
            // that appear inside elements, e.g., <a href="@url">
            context.CodeWriter
                .WriteStartMethodInvocation($"{builderVarName}.{nameof(RenderTreeBuilder.AddText)}");

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                {
                    context.CodeWriter.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the expression like a Template or another extension node.
                    throw new NotImplementedException("Unsupported: CSharpExpression with child node that isn't a CSharp node");
                }
            }

            context.CodeWriter
                .WriteEndMethodInvocation();
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
            var originalHtmlContent = GetContent(node);
            var tokenizer = new HtmlTokenizer(
                new TextSource(originalHtmlContent),
                HtmlEntityService.Resolver);

            HtmlToken nextToken;
            while ((nextToken = tokenizer.Get()).Type != HtmlTokenType.EndOfFile)
            {
                switch (nextToken.Type)
                {
                    case HtmlTokenType.Character:
                        {
                            // Text node
                            context.CodeWriter
                                .WriteStartMethodInvocation($"{builderVarName}.{nameof(RenderTreeBuilder.AddText)}")
                                .WriteStringLiteral(nextToken.Data)
                                .WriteEndMethodInvocation();
                            break;
                        }

                    case HtmlTokenType.StartTag:
                    case HtmlTokenType.EndTag:
                        {
                            var nextTag = nextToken.AsTag();
                            if (nextToken.Type == HtmlTokenType.StartTag)
                            {
                                context.CodeWriter
                                    .WriteStartMethodInvocation($"{builderVarName}.{nameof(RenderTreeBuilder.OpenElement)}")
                                    .WriteStringLiteral(nextTag.Data)
                                    .WriteEndMethodInvocation();
                            }

                            foreach (var attribute in nextTag.Attributes)
                            {
                                context.CodeWriter
                                    .WriteStartMethodInvocation($"{builderVarName}.{nameof(RenderTreeBuilder.AddAttribute)}")
                                    .WriteStringLiteral(attribute.Key)
                                    .WriteParameterSeparator()
                                    .WriteStringLiteral(attribute.Value)
                                    .WriteEndMethodInvocation();
                            }

                            if (nextToken.Type == HtmlTokenType.EndTag
                                || nextTag.IsSelfClosing
                                || htmlVoidElementsLookup.Contains(nextTag.Data))
                            {
                                context.CodeWriter
                                    .WriteStartMethodInvocation($"{builderVarName}.{nameof(RenderTreeBuilder.CloseElement)}")
                                    .WriteEndMethodInvocation();
                            }
                            break;
                        }

                    default:
                        throw new InvalidCastException($"Unsupported token type: {nextToken.Type.ToString()}");
                }
            }

            
        }

        public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
        {
            throw new System.NotImplementedException(nameof(WriteUsingDirective));
        }

        private static string GetContent(HtmlContentIntermediateNode node)
        {
            var builder = new StringBuilder();
            var htmlTokens = node.Children.OfType<IntermediateToken>().Where(t => t.IsHtml);
            foreach (var htmlToken in htmlTokens)
            {
                builder.Append(htmlToken.Content);
            }
            return builder.ToString();
        }
    }
}
