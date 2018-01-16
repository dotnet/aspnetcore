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

        private string _unconsumedHtml;
        private IList<object> _currentAttributeValues;
        private IDictionary<string, object> _currentElementAttributes = new Dictionary<string, object>();

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
            if (_currentAttributeValues == null)
            {
                throw new InvalidOperationException($"Invoked {nameof(WriteCSharpCodeAttributeValue)} while {nameof(_currentAttributeValues)} was null.");
            }

            // For attributes like "onsomeevent=@{ /* some C# code */ }", we treat it as if you
            // wrote "onsomeevent=@(_ => { /* some C# code */ })" because then it works as an
            // event handler and is a reasonable syntax for that.
            var innerCSharp = (IntermediateToken)node.Children.Single();
            innerCSharp.Content = $"_ => {{ {innerCSharp.Content} }}";
            _currentAttributeValues.Add(innerCSharp);
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
            if (_currentAttributeValues == null)
            {
                throw new InvalidOperationException($"Invoked {nameof(WriteCSharpCodeAttributeValue)} while {nameof(_currentAttributeValues)} was null.");
            }

            // In cases like "somestring @variable", Razor tokenizes it as:
            //  [0] HtmlContent="somestring"
            //  [1] CsharpContent="variable" Prefix=" "
            // ... so to avoid losing whitespace, convert the prefix to a further token in the list
            if (!string.IsNullOrEmpty(node.Prefix))
            {
                _currentAttributeValues.Add(node.Prefix);
            }

            _currentAttributeValues.Add((IntermediateToken)node.Children.Single());
        }

        public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
        {
            _currentAttributeValues = new List<object>();
            context.RenderChildren(node);
            _currentElementAttributes[node.AttributeName] = _currentAttributeValues;
            _currentAttributeValues = null;
        }

        public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node)
        {
            if (_currentAttributeValues == null)
            {
                throw new InvalidOperationException($"Invoked {nameof(WriteHtmlAttributeValue)} while {nameof(_currentAttributeValues)} was null.");
            }

            var stringContent = ((IntermediateToken)node.Children.Single()).Content;
            _currentAttributeValues.Add(node.Prefix + stringContent);
        }

        public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node)
        {
            var originalHtmlContent = GetContent(node);
            if (_unconsumedHtml != null)
            {
                originalHtmlContent = _unconsumedHtml + originalHtmlContent;
                _unconsumedHtml = null;
            }

            var tokenizer = new HtmlTokenizer(
                new TextSource(originalHtmlContent),
                HtmlEntityService.Resolver);
            var codeWriter = context.CodeWriter;

            HtmlToken nextToken;
            while ((nextToken = tokenizer.Get()).Type != HtmlTokenType.EndOfFile)
            {
                switch (nextToken.Type)
                {
                    case HtmlTokenType.Character:
                        {
                            // Text node
                            codeWriter
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
                                codeWriter
                                    .WriteStartMethodInvocation($"{builderVarName}.{nameof(RenderTreeBuilder.OpenElement)}")
                                    .WriteStringLiteral(nextTag.Data)
                                    .WriteEndMethodInvocation();
                            }

                            foreach (var attribute in nextTag.Attributes)
                            {
                                WriteAttribute(codeWriter, attribute.Key, attribute.Value);
                            }

                            if (_currentElementAttributes.Count > 0)
                            {
                                foreach (var pair in _currentElementAttributes)
                                {
                                    WriteAttribute(codeWriter, pair.Key, pair.Value);
                                }
                                _currentElementAttributes.Clear();
                            }

                            if (nextToken.Type == HtmlTokenType.EndTag
                                || nextTag.IsSelfClosing
                                || htmlVoidElementsLookup.Contains(nextTag.Data))
                            {
                                codeWriter
                                    .WriteStartMethodInvocation($"{builderVarName}.{nameof(RenderTreeBuilder.CloseElement)}")
                                    .WriteEndMethodInvocation();
                            }
                            break;
                        }

                    default:
                        throw new InvalidCastException($"Unsupported token type: {nextToken.Type.ToString()}");
                }
            }

            // If we got an EOF in the middle of an HTML element, it's probably because we're
            // about to receive some attribute name/value pairs. Store the unused HTML content
            // so we can prepend it to the part that comes after the attributes to make
            // complete valid markup.
            if (originalHtmlContent.Length > nextToken.Position.Position)
            {
                _unconsumedHtml = originalHtmlContent.Substring(nextToken.Position.Position - 1);
            }
        }

        private static void WriteAttribute(CodeWriter codeWriter, string key, object value)
        {
            codeWriter
                .WriteStartMethodInvocation($"{builderVarName}.{nameof(RenderTreeBuilder.AddAttribute)}")
                .WriteStringLiteral(key)
                .WriteParameterSeparator();
            WriteAttributeValue(codeWriter, value);
            codeWriter.WriteEndMethodInvocation();
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

        private static void WriteAttributeValue(CodeWriter writer, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            switch (value)
            {
                case string valueString:
                    writer.WriteStringLiteral(valueString);
                    break;
                case IntermediateToken token:
                    {
                        if (token.IsCSharp)
                        {
                            writer.Write(token.Content);
                        }
                        else
                        {
                            writer.WriteStringLiteral(token.Content);
                        }
                        break;
                    }
                case IEnumerable<object> concatenatedValues:
                    {
                        var first = true;
                        foreach (var concatenatedValue in concatenatedValues)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                writer.Write(" + ");
                            }

                            WriteAttributeValue(writer, concatenatedValue);
                        }
                        break;
                    }
                default:
                    throw new ArgumentException($"Unsupported attribute value type: {value.GetType().FullName}");
            }
        }
    }
}
