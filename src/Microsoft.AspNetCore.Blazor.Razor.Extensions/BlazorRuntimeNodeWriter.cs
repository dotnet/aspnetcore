// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Blazor.Shared;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    /// <summary>
    /// Generates the C# code corresponding to Razor source document contents.
    /// </summary>
    internal class BlazorRuntimeNodeWriter : BlazorNodeWriter
    {
        private readonly List<IntermediateToken> _currentAttributeValues = new List<IntermediateToken>();
        private readonly ScopeStack _scopeStack = new ScopeStack();
        private int _sourceSequence = 0;

        public override void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

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
                // The runtime and design time code differ in their handling of whitespace-only
                // statements. At runtime we can discard them completely. At design time we need
                // to keep them for the editor.
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
                    _scopeStack.IncrementCurrentScopeChildCount(context);
                    context.AddSourceMappingFor(token);
                    context.CodeWriter.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the statement like an extension node.
                    context.RenderNode(node.Children[i]);
                }
            }

            if (linePragmaScope != null)
            {
                linePragmaScope.Dispose();
            }
            else
            {
                context.CodeWriter.WriteLine();
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

            context.CodeWriter.WriteEndMethodInvocation();
        }

        public override void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // In cases like "somestring @variable", Razor tokenizes it as:
            //  [0] HtmlContent="somestring"
            //  [1] CsharpContent="variable" Prefix=" "
            // ... so to avoid losing whitespace, convert the prefix to a further token in the list
            if (!string.IsNullOrEmpty(node.Prefix))
            {
                _currentAttributeValues.Add(new IntermediateToken() { Kind = TokenKind.Html, Content = node.Prefix });
            }

            for (var i = 0; i < node.Children.Count; i++)
            {
                _currentAttributeValues.Add((IntermediateToken)node.Children[i]);
            }
        }

        public override void WriteHtmlElement(CodeRenderingContext context, HtmlElementIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            context.CodeWriter
                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(BlazorApi.RenderTreeBuilder.OpenElement)}")
                .Write((_sourceSequence++).ToString())
                .WriteParameterSeparator()
                .WriteStringLiteral(node.TagName)
                .WriteEndMethodInvocation();

            // Render Attributes before creating the scope.
            foreach (var attribute in node.Attributes)
            {
                context.RenderNode(attribute);
            }

            foreach (var capture in node.Captures)
            {
                context.RenderNode(capture);
            }

            _scopeStack.OpenScope(tagName: node.TagName, isComponent: false);

            // Render body of the tag inside the scope
            foreach (var child in node.Body)
            {
                context.RenderNode(child);
            }

            _scopeStack.CloseScope(context);

            context.CodeWriter
                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{BlazorApi.RenderTreeBuilder.CloseElement}")
                .WriteEndMethodInvocation();
        }

        public override void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            Debug.Assert(_currentAttributeValues.Count == 0);
            context.RenderChildren(node);

            WriteAttribute(context.CodeWriter, node.AttributeName, _currentAttributeValues);
            _currentAttributeValues.Clear();
        }

        public override void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            var stringContent = ((IntermediateToken)node.Children.Single()).Content;
            _currentAttributeValues.Add(new IntermediateToken() { Kind = TokenKind.Html, Content = node.Prefix + stringContent, });
        }

        public override void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // Text node
            var content = GetHtmlContent(node);
            _scopeStack.IncrementCurrentScopeChildCount(context);
            context.CodeWriter
                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(BlazorApi.RenderTreeBuilder.AddContent)}")
                .Write((_sourceSequence++).ToString())
                .WriteParameterSeparator()
                .WriteStringLiteral(content)
                .WriteEndMethodInvocation();
        }

        public override void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            context.CodeWriter.WriteUsing(node.Content, endLine: true);
        }

        public override void WriteComponent(CodeRenderingContext context, ComponentExtensionNode node)
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

            foreach (var attribute in node.Attributes)
            {
                context.RenderNode(attribute);
            }

            _scopeStack.OpenScope(node.TagName, isComponent: true);
            foreach (var child in node.Body)
            {
                context.RenderNode(child);
            }
            _scopeStack.CloseScope(context);

            foreach (var capture in node.Captures)
            {
                context.RenderNode(capture);
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
            else if (node.Children.Count > 1)
            {
                // We don't expect this to happen, we just want to know if it can.
                throw new InvalidOperationException("Attribute nodes should either be minimized or a single type of content." + string.Join(", ", node.Children));
            }
            else if (node.Children.Count == 1 && node.Children[0] is HtmlContentIntermediateNode htmlNode)
            {
                // This is how string attributes are lowered by default, a single HTML node with a single HTML token.
                var content = string.Join(string.Empty, GetHtmlTokens(htmlNode).Select(t => t.Content));
                context.CodeWriter.WriteStringLiteral(content);
            }
            else
            {
                // See comments in BlazorDesignTimeNodeWriter for a description of the cases that are possible.
                var tokens = GetCSharpTokens(node);
                if (node.BoundAttribute?.IsDelegateProperty() ?? false)
                {
                    context.CodeWriter.Write("new ");
                    context.CodeWriter.Write(node.BoundAttribute.TypeName);
                    context.CodeWriter.Write("(");

                    for (var i = 0; i < tokens.Count; i++)
                    {
                        context.CodeWriter.Write(tokens[i].Content);
                    }

                    context.CodeWriter.Write(")");
                }
                else
                {
                    for (var i = 0; i < tokens.Count; i++)
                    {
                        context.CodeWriter.Write(tokens[i].Content);
                    }
                }
            }

            context.CodeWriter.Write(");");
            context.CodeWriter.WriteLine();

            IReadOnlyList<IntermediateToken> GetCSharpTokens(ComponentAttributeExtensionNode attribute)
            {
                // We generally expect all children to be CSharp, this is here just in case.
                return attribute.FindDescendantNodes<IntermediateToken>().Where(t => t.IsCSharp).ToArray();
            }

            IReadOnlyList<IntermediateToken> GetHtmlTokens(HtmlContentIntermediateNode html)
            {
                // We generally expect all children to be HTML, this is here just in case.
                return html.FindDescendantNodes<IntermediateToken>().Where(t => t.IsHtml).ToArray();
            }
        }

        public override void WriteReferenceCapture(CodeRenderingContext context, RefExtensionNode node)
        {
            var codeWriter = context.CodeWriter;

            var methodName = node.IsComponentCapture
                ? nameof(BlazorApi.RenderTreeBuilder.AddComponentReferenceCapture)
                : nameof(BlazorApi.RenderTreeBuilder.AddElementReferenceCapture);
            codeWriter
                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{methodName}")
                .Write((_sourceSequence++).ToString())
                .WriteParameterSeparator();

            const string refCaptureParamName = "__value";
            using (var lambdaScope = codeWriter.BuildLambda(refCaptureParamName))
            {
                var typecastIfNeeded = node.IsComponentCapture ? $"({node.ComponentCaptureTypeName})" : string.Empty;
                WriteCSharpCode(context, new CSharpCodeIntermediateNode
                {
                    Source = node.Source,
                    Children =
                    {
                        node.IdentifierToken,
                        new IntermediateToken {
                            Kind = TokenKind.CSharp,
                            Content = $" = {typecastIfNeeded}{refCaptureParamName};"
                        }
                    }
                });
            }

            codeWriter.WriteEndMethodInvocation();
        }

        private void WriteAttribute(CodeWriter codeWriter, string key, IList<IntermediateToken> value)
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

        private static string GetHtmlContent(HtmlContentIntermediateNode node)
        {
            var builder = new StringBuilder();
            var htmlTokens = node.Children.OfType<IntermediateToken>().Where(t => t.IsHtml);
            foreach (var htmlToken in htmlTokens)
            {
                builder.Append(htmlToken.Content);
            }
            return builder.ToString();
        }

        // There are a few cases here, we need to handle:
        // - Pure HTML
        // - Pure CSharp
        // - Mixed HTML and CSharp
        //
        // Only the mixed case is complicated, we want to turn it into code that will concatenate
        // the values into a string at runtime.

        private static void WriteAttributeValue(CodeWriter writer, IList<IntermediateToken> tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            var hasHtml = false;
            var hasCSharp = false;
            for (var i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].IsCSharp)
                {
                    hasCSharp |= true;
                }
                else
                {
                    hasHtml |= true;
                }
            }

            if (hasHtml && hasCSharp)
            {
                // If it's a C# expression, we have to wrap it in parens, otherwise things like ternary 
                // expressions don't compose with concatenation. However, this is a little complicated
                // because C# tokens themselves aren't guaranteed to be distinct expressions. We want
                // to treat all contiguous C# tokens as a single expression.
                var insideCSharp = false;
                for (var i = 0; i < tokens.Count; i++)
                {
                    var token = tokens[i];
                    if (token.IsCSharp)
                    {
                        if (!insideCSharp)
                        {
                            if (i != 0)
                            {
                                writer.Write(" + ");
                            }

                            writer.Write("(");
                            insideCSharp = true;
                        }

                        writer.Write(token.Content);
                    }
                    else
                    {
                        if (insideCSharp)
                        {
                            writer.Write(")");
                            insideCSharp = false;
                        }

                        if (i != 0)
                        {
                            writer.Write(" + ");
                        }

                        writer.WriteStringLiteral(token.Content);
                    }
                }

                if (insideCSharp)
                {
                    writer.Write(")");
                }
            }
            else if (hasCSharp)
            {
                writer.Write(string.Join("", tokens.Select(t => t.Content)));
            }
            else if (hasHtml)
            {
                writer.WriteStringLiteral(string.Join("", tokens.Select(t => t.Content)));
            }
            else
            {
                // Minimized attributes always map to 'true'
                writer.Write("true");
            }
        }
    }
}
