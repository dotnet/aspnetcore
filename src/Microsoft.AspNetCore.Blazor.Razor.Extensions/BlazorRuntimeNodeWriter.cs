// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Parser.Html;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// Generates the C# code corresponding to Razor source document contents.
    /// </summary>
    internal class BlazorRuntimeNodeWriter : BlazorNodeWriter
    {
        // Per the HTML spec, the following elements are inherently self-closing
        // For example, <img> is the same as <img /> (and therefore it cannot contain descendants)
        private readonly static HashSet<string> htmlVoidElementsLookup
            = new HashSet<string>(
                new[] { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" },
                StringComparer.OrdinalIgnoreCase);
        private readonly static Regex bindExpressionRegex = new Regex(@"^bind\((.+)\)$");
        private readonly static CSharpParseOptions bindArgsParseOptions
            = CSharpParseOptions.Default.WithKind(CodeAnalysis.SourceCodeKind.Script);

        private readonly ScopeStack _scopeStack = new ScopeStack();
        private string _unconsumedHtml;
        private IList<object> _currentAttributeValues;
        private IDictionary<string, PendingAttribute> _currentElementAttributes = new Dictionary<string, PendingAttribute>();
        private IList<PendingAttributeToken> _currentElementAttributeTokens = new List<PendingAttributeToken>();
        private int _sourceSequence = 0;

        private struct PendingAttribute
        {
            public object AttributeValue;
        }

        private struct PendingAttributeToken
        {
            public IntermediateToken AttributeValue;
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
                    _scopeStack.IncrementCurrentScopeChildCount(context);
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
            // To support syntax like <elem @completeAttributePair /> (which in turn supports syntax
            // like <elem @OnSomeEvent(Handler) />), check whether we are currently in the middle of
            // writing an element. If so, treat this C# expression as something that should evaluate
            // as a RenderTreeFrame of type Attribute.
            if (_unconsumedHtml != null)
            {
                var token = (IntermediateToken)node.Children.Single();
                _currentElementAttributeTokens.Add(new PendingAttributeToken
                {
                    AttributeValue = token
                });
                return;
            }

            // Since we're not in the middle of writing an element, this must evaluate as some
            // text to display
            _scopeStack.IncrementCurrentScopeChildCount(context);
            context.CodeWriter
                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(BlazorApi.RenderTreeBuilder.AddContent)}")
                .Write((_sourceSequence++).ToString())
                .WriteParameterSeparator();

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
            _currentElementAttributes[node.AttributeName] = new PendingAttribute
            {
                AttributeValue = _currentAttributeValues
            };
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

            // TODO: As an optimization, identify static subtrees (i.e., HTML elements in the Razor source
            // that contain no C#) and represent them as a new RenderTreeFrameType called StaticElement or
            // similar. This means you can have arbitrarily deep static subtrees without paying any per-
            // node cost during rendering or diffing.
            HtmlToken nextToken;
            while ((nextToken = tokenizer.Get()).Type != HtmlTokenType.EndOfFile)
            {
                switch (nextToken.Type)
                {
                    case HtmlTokenType.Character:
                        {
                            // Text node
                            _scopeStack.IncrementCurrentScopeChildCount(context);
                            codeWriter
                                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(BlazorApi.RenderTreeBuilder.AddContent)}")
                                .Write((_sourceSequence++).ToString())
                                .WriteParameterSeparator()
                                .WriteStringLiteral(nextToken.Data)
                                .WriteEndMethodInvocation();
                            break;
                        }

                    case HtmlTokenType.StartTag:
                    case HtmlTokenType.EndTag:
                        {
                            var nextTag = nextToken.AsTag();
                            var tagNameOriginalCase = GetTagNameWithOriginalCase(originalHtmlContent, nextTag);

                            if (nextToken.Type == HtmlTokenType.StartTag)
                            {
                                _scopeStack.IncrementCurrentScopeChildCount(context);

                                codeWriter
                                    .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(BlazorApi.RenderTreeBuilder.OpenElement)}")
                                    .Write((_sourceSequence++).ToString())
                                    .WriteParameterSeparator()
                                    .WriteStringLiteral(nextTag.Data)
                                    .WriteEndMethodInvocation();
 
                                foreach (var attribute in nextTag.Attributes)
                                {
                                    WriteAttribute(codeWriter, attribute.Key, attribute.Value);
                                }

                                if (_currentElementAttributes.Count > 0)
                                {
                                    foreach (var pair in _currentElementAttributes)
                                    {
                                        WriteAttribute(codeWriter, pair.Key, pair.Value.AttributeValue);
                                    }
                                    _currentElementAttributes.Clear();
                                }

                                if (_currentElementAttributeTokens.Count > 0)
                                {
                                    foreach (var token in _currentElementAttributeTokens)
                                    {
                                        WriteElementAttributeToken(context, nextTag, token);
                                    }
                                    _currentElementAttributeTokens.Clear();
                                }

                                _scopeStack.OpenScope( tagName: nextTag.Data, isComponent: false);
                            }

                            if (nextToken.Type == HtmlTokenType.EndTag
                                || nextTag.IsSelfClosing
                                || htmlVoidElementsLookup.Contains(nextTag.Data))
                            {
                                _scopeStack.CloseScope(
                                    context: context,
                                    tagName: nextTag.Data,
                                    isComponent: false,
                                    source: CalculateSourcePosition(node.Source, nextToken.Position));
                                codeWriter
                                    .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{BlazorApi.RenderTreeBuilder.CloseElement}")
                                    .WriteEndMethodInvocation();
                            }
                            break;
                        }

                    case HtmlTokenType.Comment:
                        break;

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

        private void WriteElementAttributeToken(CodeRenderingContext context, HtmlTagToken tag, PendingAttributeToken token)
        {
            var bindMatch = bindExpressionRegex.Match(token.AttributeValue.Content);
            if (bindMatch.Success)
            {
                // TODO: Consider alternatives to the @bind syntax. The following is very strange.

                // The @bind(X, Y, Z, ...) syntax is special. We convert it to a pair of attributes:
                // [1] value=@BindMethods.GetValue(X, Y, Z, ...)
                var valueParams = bindMatch.Groups[1].Value;
                WriteAttribute(context.CodeWriter, "value", new IntermediateToken
                {
                    Kind = TokenKind.CSharp,
                    Content = $"{BlazorApi.BindMethods.GetValue}({valueParams})"
                });

                // [2] @onchange(BindSetValue(parsed => { X = parsed; }, X, Y, Z, ...))
                var parsedArgs = CSharpSyntaxTree.ParseText(valueParams, bindArgsParseOptions);
                var parsedArgsSplit = parsedArgs.GetRoot().ChildNodes().Select(x => x.ToString()).ToList();
                if (parsedArgsSplit.Count > 0)
                {
                    parsedArgsSplit.Insert(0, $"_parsedValue_ => {{ {parsedArgsSplit[0]} = _parsedValue_; }}");
                }
                var parsedArgsJoined = string.Join(", ", parsedArgsSplit);
                var onChangeAttributeToken = new PendingAttributeToken
                {
                    AttributeValue = new IntermediateToken
                    {
                        Kind = TokenKind.CSharp,
                        Content = $"onchange({BlazorApi.BindMethods.SetValue}({parsedArgsJoined}))"
                    }
                };
                WriteElementAttributeToken(context, tag, onChangeAttributeToken);
            }
            else
            {
                // For any other attribute token (e.g., @onclick(...)), treat it as an expression
                // that will evaluate as an attribute frame
                context.CodeWriter
                    .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(BlazorApi.RenderTreeBuilder.AddAttribute)}")
                    .Write((_sourceSequence++).ToString())
                    .WriteParameterSeparator()
                    .Write(token.AttributeValue.Content)
                    .WriteEndMethodInvocation();
            }
        }

        public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
        {
            context.CodeWriter.WriteUsing(node.Content, endLine: true);
        }

        public override void WriteComponentOpen(CodeRenderingContext context, ComponentOpenExtensionNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // The start tag counts as a child from a markup point of view.
            _scopeStack.IncrementCurrentScopeChildCount(context);

            // builder.OpenComponent<TComponent>(42);
            context.CodeWriter.Write(_scopeStack.BuilderVarName);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(BlazorApi.RenderTreeBuilder.OpenComponent);
            context.CodeWriter.Write("<");
            context.CodeWriter.Write(node.TypeName);
            context.CodeWriter.Write(">(");
            context.CodeWriter.Write((_sourceSequence++).ToString());
            context.CodeWriter.Write(");");
            context.CodeWriter.WriteLine();
        }

        public override void WriteComponentClose(CodeRenderingContext context, ComponentCloseExtensionNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // The close tag counts as a child from a markup point of view.
            _scopeStack.IncrementCurrentScopeChildCount(context);

            // builder.OpenComponent<TComponent>(42);
            context.CodeWriter.Write(_scopeStack.BuilderVarName);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(BlazorApi.RenderTreeBuilder.CloseComponent);
            context.CodeWriter.Write("();");
            context.CodeWriter.WriteLine();
        }

        public override void WriteComponentBody(CodeRenderingContext context, ComponentBodyExtensionNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            _scopeStack.OpenScope(node.TagName, isComponent: true);
            context.RenderChildren(node);
            _scopeStack.CloseScope(context, node.TagName, isComponent: true, source: node.Source);
        }

        public override void WriteComponentAttribute(CodeRenderingContext context, ComponentAttributeExtensionNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // builder.OpenComponent<TComponent>(42);
            context.CodeWriter.Write(_scopeStack.BuilderVarName);
            context.CodeWriter.Write(".");
            context.CodeWriter.Write(BlazorApi.RenderTreeBuilder.AddAttribute);
            context.CodeWriter.Write("(");
            context.CodeWriter.Write((_sourceSequence++).ToString());
            context.CodeWriter.Write(", ");
            context.CodeWriter.WriteStringLiteral(node.AttributeName);
            context.CodeWriter.Write(", ");

            if (node.AttributeStructure == AttributeStructure.Minimized)
            {
                // Minimized attributes always map to 'true'
                context.CodeWriter.Write("true");
            }
            else if (
                node.Children.Count != 1 ||
                node.Children[0] is HtmlContentIntermediateNode htmlNode && htmlNode.Children.Count != 1 ||
                node.Children[0] is CSharpExpressionIntermediateNode cSharpNode && cSharpNode.Children.Count != 1)
            {
                // We don't expect this to happen, we just want to know if it can.
                throw new InvalidOperationException("Attribute nodes should either be minimized or a single content node.");
            }
            else if (node.BoundAttribute?.IsDelegateProperty() ?? false)
            {
                // We always surround the expression with the delegate constructor. This makes type
                // inference inside lambdas, and method group conversion do the right thing.
                IntermediateToken token = null;
                if ((cSharpNode = node.Children[0] as CSharpExpressionIntermediateNode) != null)
                {
                    token = cSharpNode.Children[0] as IntermediateToken;
                }
                else
                {
                    token = node.Children[0] as IntermediateToken;
                }

                if (token != null)
                {
                    context.CodeWriter.Write("new ");
                    context.CodeWriter.Write(node.BoundAttribute.TypeName);
                    context.CodeWriter.Write("(");
                    context.CodeWriter.Write(token.Content);
                    context.CodeWriter.Write(")");
                }
            }
            else if ((cSharpNode = node.Children[0] as CSharpExpressionIntermediateNode) != null)
            {
                context.CodeWriter.Write(((IntermediateToken)cSharpNode.Children[0]).Content);
            }
            else if ((htmlNode = node.Children[0] as HtmlContentIntermediateNode) != null)
            {
                // This is how string attributes are lowered by default, a single HTML node with a single HTML token.
                context.CodeWriter.WriteStringLiteral(((IntermediateToken)htmlNode.Children[0]).Content);
            }
            else if (node.Children[0] is IntermediateToken token)
            {
                // This is what we expect for non-string nodes.
                context.CodeWriter.Write(((IntermediateToken)node.Children[0]).Content);
            }
            else
            {
                throw new InvalidOperationException("Unexpected node type " + node.Children[0].GetType().FullName);
            }
            
            context.CodeWriter.Write(");");
            context.CodeWriter.WriteLine();
        }

        private SourceSpan? CalculateSourcePosition(
            SourceSpan? razorTokenPosition,
            TextPosition htmlNodePosition)
        {
            if (razorTokenPosition.HasValue)
            {
                var razorPos = razorTokenPosition.Value;
                return new SourceSpan(
                    razorPos.FilePath,
                    razorPos.AbsoluteIndex + htmlNodePosition.Position,
                    razorPos.LineIndex + htmlNodePosition.Line - 1,
                    htmlNodePosition.Line == 1
                        ? razorPos.CharacterIndex + htmlNodePosition.Column - 1
                        : htmlNodePosition.Column - 1,
                    length: 1);
            }
            else
            {
                return null;
            }
        }

        private static string GetTagNameWithOriginalCase(string document, HtmlTagToken tagToken)
        {
            var offset = tagToken.Type == HtmlTokenType.EndTag ? 1 : 0; // For end tags, skip the '/'
            return document.Substring(tagToken.Position.Position + offset, tagToken.Name.Length);
        }

        private void WriteAttribute(CodeWriter codeWriter, string key, object value)
        {
            BeginWriteAttribute(codeWriter, key);
            WriteAttributeValue(codeWriter, value);
            codeWriter.WriteEndMethodInvocation();
        }

        public override void BeginWriteAttribute(CodeWriter codeWriter, string key)
        {
            // Temporary workaround for https://github.com/aspnet/Blazor/issues/219
            // Remove this logic once the underlying HTML parsing issue is fixed,
            // as we don't really want special cases like this.
            const string dataUnderscore = "data_";
            if (key.StartsWith(dataUnderscore, StringComparison.Ordinal))
            {
                key = "data-" + key.Substring(dataUnderscore.Length);
            }

            codeWriter
                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(BlazorApi.RenderTreeBuilder.AddAttribute)}")
                .Write((_sourceSequence++).ToString())
                .WriteParameterSeparator()
                .WriteStringLiteral(key)
                .WriteParameterSeparator();
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
