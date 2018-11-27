// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AngleSharp;
using AngleSharp.Extensions;
using AngleSharp.Html;
using AngleSharp.Parser.Html;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Components.Razor
{
    // Rewrites the standard IR to a format more suitable for Blazor
    //
    // HTML nodes are rewritten to contain more structure, instead of treating HTML as opaque content
    // it is structured into element/component nodes, and attribute nodes.
    internal class ComponentDocumentRewritePass : IntermediateNodePassBase, IRazorDocumentClassifierPass
    {
        // Per the HTML spec, the following elements are inherently self-closing
        // For example, <img> is the same as <img /> (and therefore it cannot contain descendants)
        public static readonly HashSet<string> VoidElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr",
        };

        // Run as soon as possible after the Component document classifier
        public override int Order => ComponentDocumentClassifierPass.DefaultFeatureOrder + 1;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            if (documentNode.DocumentKind != ComponentDocumentClassifierPass.ComponentDocumentKind)
            {
                return;
            }

            var visitor = new RewriteWalker(codeDocument.Source);
            visitor.Visit(documentNode);
        }

        // Visits nodes then rewrites them using a post-order traversal. The result is that the tree
        // is rewritten bottom up.
        //
        // This relies on a few invariants Razor already provides for correctness.
        // - Tag Helpers are the only real nesting construct
        // - Tag Helpers require properly nested HTML inside their body
        //
        // This means that when we find a 'container' for HTML content, we have the guarantee
        // that the content is properly nested, except at the top level of scope. And since the top
        // level isn't nested inside anything, we can't introduce any errors due to misunderstanding
        // the structure.
        private class RewriteWalker : IntermediateNodeWalker
        {
            private readonly RazorSourceDocument _source;

            public RewriteWalker(RazorSourceDocument source)
            {
                _source = source;
            }

            public override void VisitDefault(IntermediateNode node)
            {
                var foundHtml = false;
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    Visit(child);

                    if (child is HtmlContentIntermediateNode)
                    {
                        foundHtml = true;
                    }
                }

                if (foundHtml)
                {
                    RewriteChildren(_source, node);
                }
            }

            public override void VisitHtmlAttribute(HtmlAttributeIntermediateNode node)
            {
                // Don't rewrite inside of attributes
            }

            public override void VisitTagHelperHtmlAttribute(TagHelperHtmlAttributeIntermediateNode node)
            {
                // Don't rewrite inside of attributes
            }

            public override void VisitTagHelperProperty(TagHelperPropertyIntermediateNode node)
            {
                // Don't rewrite inside of attributes
            }

            private void RewriteChildren(RazorSourceDocument source, IntermediateNode node)
            {
                // We expect all of the immediate children of a node (together) to comprise
                // a well-formed tree of elements and components. 
                var stack = new Stack<IntermediateNode>();
                stack.Push(node);

                // Make a copy, we will clear and rebuild the child collection of this node.
                var children = node.Children.ToArray();
                node.Children.Clear();

                // Due to the way Anglesharp parses HTML (tags at a time) we need to keep track of some state.
                // This handles cases like:
                //
                //  <foo bar="17" baz="@baz" />
                //
                // This will lower like:
                //
                //  HtmlContent <foo bar="17"
                //  HtmlAttribute baz=" - "
                //      CSharpAttributeValue baz
                //  HtmlContent  />
                //
                // We need to consume HTML until we see the 'end tag' for <foo /> and then we can 
                // the attributes from the parsed HTML and the CSharpAttribute value.
                var parser = new HtmlParser(source);
                var attributes = new List<HtmlAttributeIntermediateNode>();

                for (var i = 0; i < children.Length; i++)
                {
                    if (children[i] is HtmlContentIntermediateNode htmlNode)
                    {
                        parser.Push(htmlNode);
                        var tokens = parser.Get();
                        foreach (var token in tokens)
                        {
                            // We have to call this before get. Anglesharp doesn't return the start position
                            // of tokens.
                            var start = parser.GetCurrentLocation();

                            // We have to set the Location explicitly otherwise we would need to include
                            // the token in every call to the parser.
                            parser.SetLocation(token);

                            var end = parser.GetCurrentLocation();

                            if (token.Type == HtmlTokenType.EndOfFile)
                            {
                                break;
                            }

                            switch (token.Type)
                            {
                                case HtmlTokenType.Doctype:
                                    {
                                        // DocType isn't meaningful in Blazor. We don't process them in the runtime
                                        // it wouldn't really mean much anyway since we build a DOM directly rather
                                        // than letting the user-agent parse the document.
                                        //
                                        // For now, <!DOCTYPE html> and similar things will just be skipped by the compiler
                                        // unless we come up with something more meaningful to do.
                                        break;
                                    }

                                case HtmlTokenType.Character:
                                    {
                                        // Text content
                                        var span = new SourceSpan(start, end.AbsoluteIndex - start.AbsoluteIndex);
                                        stack.Peek().Children.Add(new HtmlContentIntermediateNode()
                                        {
                                            Children =
                                            {
                                                new IntermediateToken()
                                                {
                                                    Content = token.Data,
                                                    Kind = TokenKind.Html,
                                                    Source = span,
                                                }
                                            },
                                            Source = span,
                                        });
                                        break;
                                    }

                                case HtmlTokenType.StartTag:
                                    {
                                        var tag = token.AsTag();

                                        if (token.Type == HtmlTokenType.StartTag)
                                        {
                                            var elementNode = new HtmlElementIntermediateNode()
                                            {
                                                TagName = parser.GetTagNameOriginalCasing(tag),
                                                Source = new SourceSpan(start, end.AbsoluteIndex - start.AbsoluteIndex),
                                            };

                                            stack.Peek().Children.Add(elementNode);
                                            stack.Push(elementNode);

                                            for (var j = 0; j < tag.Attributes.Count; j++)
                                            {
                                                // Unfortunately Anglesharp doesn't provide positions for attributes
                                                // so we can't record the spans here.
                                                var attribute = tag.Attributes[j];
                                                stack.Peek().Children.Add(CreateAttributeNode(attribute));
                                            }

                                            for (var j = 0; j < attributes.Count; j++)
                                            {
                                                stack.Peek().Children.Add(attributes[j]);
                                            }
                                            attributes.Clear();
                                        }

                                        if (tag.IsSelfClosing || VoidElements.Contains(tag.Data))
                                        {
                                            // We can't possibly hit an error here since we just added an element node.
                                            stack.Pop();
                                        }

                                        break;
                                    }

                                case HtmlTokenType.EndTag:
                                    {
                                        var tag = token.AsTag();

                                        var popped = stack.Pop();
                                        if (stack.Count == 0)
                                        {
                                            // If we managed to 'bottom out' the stack then we have an unbalanced end tag.
                                            // Put back the current node so we don't crash.
                                            stack.Push(popped);

                                            var tagName = parser.GetTagNameOriginalCasing(tag);
                                            var span = new SourceSpan(start, end.AbsoluteIndex - start.AbsoluteIndex);

                                            var diagnostic = VoidElements.Contains(tagName)
                                                ? BlazorDiagnosticFactory.Create_UnexpectedClosingTagForVoidElement(span, tagName)
                                                : BlazorDiagnosticFactory.Create_UnexpectedClosingTag(span, tagName);
                                            popped.Children.Add(new HtmlElementIntermediateNode()
                                            {
                                                Diagnostics =
                                                {
                                                    diagnostic,
                                                },
                                                TagName = tagName,
                                                Source = span,
                                            });
                                        }
                                        else if (!string.Equals(tag.Name, ((HtmlElementIntermediateNode)popped).TagName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            var span = new SourceSpan(start, end.AbsoluteIndex - start.AbsoluteIndex);
                                            var diagnostic = BlazorDiagnosticFactory.Create_MismatchedClosingTag(span, ((HtmlElementIntermediateNode)popped).TagName, token.Data);
                                            popped.Diagnostics.Add(diagnostic);
                                        }
                                        else
                                        {
                                            // Happy path.
                                            //
                                            // We need to compute a new source span because when we found the start tag before we knew
                                            // the end position of the tag.
                                            var length = end.AbsoluteIndex - popped.Source.Value.AbsoluteIndex;
                                            popped.Source = new SourceSpan(
                                                popped.Source.Value.FilePath,
                                                popped.Source.Value.AbsoluteIndex,
                                                popped.Source.Value.LineIndex,
                                                popped.Source.Value.CharacterIndex,
                                                length);
                                        }

                                        break;
                                    }

                                case HtmlTokenType.Comment:
                                    break;

                                default:
                                    throw new InvalidCastException($"Unsupported token type: {token.Type.ToString()}");
                            }
                        }
                    }
                    else if (children[i] is HtmlAttributeIntermediateNode htmlAttribute)
                    {
                        // Buffer the attribute for now, it will get written out as part of a tag.
                        attributes.Add(htmlAttribute);
                    }
                    else
                    {
                        // not HTML, or already rewritten.
                        stack.Peek().Children.Add(children[i]);
                    }
                }

                var extraContent = parser.GetUnparsedContent();
                if (!string.IsNullOrEmpty(extraContent))
                {
                    // extra HTML - almost certainly invalid because it couldn't be parsed.
                    var start = parser.GetCurrentLocation();
                    var end = parser.GetCurrentLocation(extraContent.Length);
                    var span = new SourceSpan(start, end.AbsoluteIndex - start.AbsoluteIndex);
                    stack.Peek().Children.Add(new HtmlContentIntermediateNode()
                    {
                        Children =
                        {
                            new IntermediateToken()
                            {
                                Content = extraContent,
                                Kind = TokenKind.Html,
                                Source = span,
                            }
                        },
                        Diagnostics =
                        {
                            BlazorDiagnosticFactory.Create_InvalidHtmlContent(span, extraContent),
                        },
                        Source = span,
                    });
                }

                while (stack.Count > 1)
                {
                    // not balanced
                    var popped = (HtmlElementIntermediateNode)stack.Pop();
                    var diagnostic = BlazorDiagnosticFactory.Create_UnclosedTag(popped.Source, popped.TagName);
                    popped.Diagnostics.Add(diagnostic);
                }
            }
        }

        private static HtmlAttributeIntermediateNode CreateAttributeNode(KeyValuePair<string, string> attribute)
        {
            return new HtmlAttributeIntermediateNode()
            {
                AttributeName = attribute.Key,
                Children =
                {
                    new HtmlAttributeValueIntermediateNode()
                    {
                        Children =
                        {
                            new IntermediateToken()
                            {
                                Kind = TokenKind.Html,
                                Content = attribute.Value,
                            },
                        }
                    },
                }
            };
        }

        private static string GetHtmlContent(HtmlContentIntermediateNode node)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < node.Children.Count; i++)
            {
                var token = node.Children[i] as IntermediateToken;
                if (token != null && token.IsHtml)
                {
                    builder.Append(token.Content);
                }
            }

            return builder.ToString();
        }

        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        private class HtmlParser
        {
            private readonly RazorSourceDocument _source;

            // Tracks the offsets between the start of _content and then original source document.
            private List<(int offset, int sourceOffset)> _offsets;
            private TextSource _textSource;
            private int _position;
            private string _content;

            public HtmlParser(RazorSourceDocument source)
            {
                _source = source;
            }

            public void Push(HtmlContentIntermediateNode node)
            {
                var builder = new StringBuilder();

                var offsets = new List<(int offset, int sourceOffset)>();

                if (_content != null && _position < _content.Length)
                {
                    offsets.Add((0, _offsets[0].sourceOffset + _position));
                    builder.Append(_content, _position, _content.Length - _position);
                }

                for (var i = 0; i < node.Children.Count; i++)
                {
                    var token = node.Children[i] as IntermediateToken;
                    if (token != null && token.IsHtml)
                    {
                        offsets.Add((builder.Length, token.Source.Value.AbsoluteIndex));
                        builder.Append(token.Content);
                    }
                }

                _content = builder.ToString();
                _offsets = offsets;
                _textSource = new TextSource(_content);
                _position = 0;
            }

            public string GetUnparsedContent()
            {
                return _position >= _content.Length ? string.Empty : _content.Substring(_position);
            }

            public IEnumerable<HtmlToken> Get()
            {
                if (_textSource == null)
                {
                    throw new InvalidOperationException("You need to call Push first.");
                }

                // This will decode any HTML entities into their textual equivalent.
                //
                // This is OK because an HtmlContent node is used with document.createTextNode
                // in the DOM, which can accept content that has decoded entities.
                //
                // In the event that we merge HtmlContent into an HtmlBlock, we need to
                // re-encode the entities. That's done in the HtmlBlock pass.
                var tokens = _textSource.Tokenize(HtmlEntityService.Resolver);
                return tokens;
            }

            public void SetLocation(HtmlToken token)
            {
                // The tokenizer will advance to the end when you have an unclosed tag.
                // We don't want this, we want to resume before the unclosed tag.
                if (token.Type != HtmlTokenType.EndOfFile)
                {
                    _position = _textSource.Index;
                }
            }

            public SourceLocation GetCurrentLocation(int offset = 0)
            {
                var absoluteIndex = GetAbsoluteIndex(_position + offset);

                int lineIndex = -1;
                int columnIndex = -1;
                var remaining = absoluteIndex;
                for (var i = 0; i < _source.Lines.Count; i++)
                {
                    var lineLength = _source.Lines.GetLineLength(i);
                    if (lineLength > remaining)
                    {
                        lineIndex = i;
                        columnIndex = remaining;
                        break;
                    }

                    remaining -= lineLength;
                }

                return new SourceLocation(_source.FilePath, absoluteIndex, lineIndex, columnIndex);
            }

            public SourceSpan GetSpan(HtmlToken token)
            {
                var absoluteIndex = GetAbsoluteIndex(token.Position.Position);

                int lineIndex = -1;
                int columnIndex = -1;
                var remaining = absoluteIndex;
                for (var i = 0; i < _source.Lines.Count; i++)
                {
                    var lineLength = _source.Lines.GetLineLength(i);
                    if (lineLength > remaining)
                    {
                        lineIndex = i;
                        columnIndex = remaining;
                        break;
                    }

                    remaining -= lineLength;
                }

                var length = GetAbsoluteIndex(_position) - absoluteIndex;
                return new SourceSpan(_source.FilePath, absoluteIndex, lineIndex, columnIndex, length);
            }

            private int GetAbsoluteIndex(int contentIndex)
            {
                for (var i = _offsets.Count - 1; i >= 0; i--)
                {
                    if (_offsets[i].offset <= contentIndex)
                    {
                        return _offsets[i].sourceOffset + (contentIndex - _offsets[i].offset);
                    }
                }

                throw new InvalidOperationException("Unexpected index value.");
            }

            // Anglesharp canonicalizes the case of tags, we want what the user typed.
            public string GetTagNameOriginalCasing(HtmlTagToken tag)
            {
                var offset = tag.Type == HtmlTokenType.EndTag ? 1 : 0; // For end tags, skip the '/'
                return tag.Name;
            }

            private string DebuggerDisplay
            {
                get
                {
                    if (_content == null)
                    {
                        return "Content={}";
                    }

                    var builder = new StringBuilder();
                    builder.Append("Content=");
                    builder.Append("{");
                    builder.Append(_content.Substring(0, Math.Min(_position, _content.Length)));
                    builder.Append("|");
                    builder.Append(_content.Substring(Math.Min(_position, _content.Length)));
                    builder.Append("}");

                    return builder.ToString();
                }
            }
        }
    }
}