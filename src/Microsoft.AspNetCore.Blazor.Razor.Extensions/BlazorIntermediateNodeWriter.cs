// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AngleSharp;
using AngleSharp.Html;
using AngleSharp.Parser.Html;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// Generates the C# code corresponding to Razor source document contents.
    /// </summary>
    internal class BlazorIntermediateNodeWriter : IntermediateNodeWriter
    {
        // Per the HTML spec, the following elements are inherently self-closing
        // For example, <img> is the same as <img /> (and therefore it cannot contain descendants)
        private static HashSet<string> htmlVoidElementsLookup
            = new HashSet<string>(
                new[] { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" },
                StringComparer.OrdinalIgnoreCase);

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
                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(RenderTreeBuilder.AddContent)}")
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
                                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(RenderTreeBuilder.AddContent)}")
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
                            var isComponent = TryGetComponentTypeNameFromTagName(tagNameOriginalCase, out var componentTypeName);

                            if (nextToken.Type == HtmlTokenType.StartTag)
                            {
                                _scopeStack.IncrementCurrentScopeChildCount(context);
                                if (isComponent)
                                {
                                    codeWriter
                                        .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(RenderTreeBuilder.OpenComponent)}<{componentTypeName}>")
                                        .Write((_sourceSequence++).ToString())
                                        .WriteEndMethodInvocation();
                                }
                                else
                                {
                                    codeWriter
                                        .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(RenderTreeBuilder.OpenElement)}")
                                        .Write((_sourceSequence++).ToString())
                                        .WriteParameterSeparator()
                                        .WriteStringLiteral(nextTag.Data)
                                        .WriteEndMethodInvocation();
                                }

                                if (isComponent && nextTag.Attributes.Count > 0)
                                {
                                    ThrowTemporaryComponentSyntaxError(node, nextTag, tagNameOriginalCase);
                                }

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
                                        codeWriter
                                            .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(RenderTreeBuilder.AddAttribute)}")
                                            .Write((_sourceSequence++).ToString())
                                            .WriteParameterSeparator()
                                            .Write(token.AttributeValue.Content)
                                            .WriteEndMethodInvocation();
                                    }
                                    _currentElementAttributeTokens.Clear();
                                }

                                _scopeStack.OpenScope(
                                    tagName: isComponent ? tagNameOriginalCase : nextTag.Data,
                                    isComponent: isComponent);
                            }

                            if (nextToken.Type == HtmlTokenType.EndTag
                                || nextTag.IsSelfClosing
                                || (!isComponent && htmlVoidElementsLookup.Contains(nextTag.Data)))
                            {
                                _scopeStack.CloseScope(
                                    context: context,
                                    tagName: isComponent ? tagNameOriginalCase : nextTag.Data,
                                    isComponent: isComponent,
                                    source: CalculateSourcePosition(node.Source, nextToken.Position));
                                var closeMethodName = isComponent
                                    ? nameof(RenderTreeBuilder.CloseComponent)
                                    : nameof(RenderTreeBuilder.CloseElement);
                                codeWriter
                                    .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{closeMethodName}")
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

        private void ThrowTemporaryComponentSyntaxError(HtmlContentIntermediateNode node, HtmlTagToken tag, string componentName)
            => throw new RazorCompilerException(
                $"Wrong syntax for '{tag.Attributes[0].Key}' on '{componentName}': As a temporary " +
                $"limitation, component attributes must be expressed with C# syntax. For example, " +
                $"SomeParam=@(\"Some value\") is allowed, but SomeParam=\"Some value\" is not.",
                CalculateSourcePosition(node.Source, tag.Position));

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

        private bool TryGetComponentTypeNameFromTagName(string tagName, out string componentTypeName)
        {
            // Determine whether 'tagName' represents a Blazor component, and if so, return the
            // name of the component's .NET type. The type name doesn't have to be fully-qualified,
            // because it's up to the developer to put in whatever @using statements are required.

            // TODO: Remove this temporary syntax and make the compiler smart enough to infer it
            // directly. This could either work by having a configurable list of non-component tag names
            // (which would default to all standard HTML elements, plus anything that contains a '-'
            // character, since those are mandatory for custom HTML elements and prohibited for .NET
            // type names), or better, could somehow know what .NET types are in scope at this point
            // in the compilation and treat everything else as a non-component element.

            const string temporaryPrefix = "c:";
            if (tagName.StartsWith(temporaryPrefix, StringComparison.Ordinal))
            {
                componentTypeName = tagName.Substring(temporaryPrefix.Length);
                return true;
            }
            else
            {
                componentTypeName = null;
                return false;
            }
        }

        private void WriteAttribute(CodeWriter codeWriter, string key, object value)
        {
            BeginWriteAttribute(codeWriter, key);
            WriteAttributeValue(codeWriter, value);
            codeWriter.WriteEndMethodInvocation();
        }

        public void BeginWriteAttribute(CodeWriter codeWriter, string key)
        {
            codeWriter
                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(RenderTreeBuilder.AddAttribute)}")
                .Write((_sourceSequence++).ToString())
                .WriteParameterSeparator()
                .WriteStringLiteral(key)
                .WriteParameterSeparator();
        }

        public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
        {
            context.CodeWriter.WriteUsing(node.Content, endLine: true);
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
