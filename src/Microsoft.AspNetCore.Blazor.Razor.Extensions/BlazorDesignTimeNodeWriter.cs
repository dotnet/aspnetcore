// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    // Based on the DesignTimeNodeWriter from Razor repo.
    internal class BlazorDesignTimeNodeWriter : BlazorNodeWriter
    {
        private readonly ScopeStack _scopeStack = new ScopeStack();

        private readonly static string DesignTimeVariable = "__o";

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

            if (node.Source.HasValue)
            {
                using (context.CodeWriter.BuildLinePragma(node.Source.Value))
                {
                    context.AddSourceMappingFor(node);
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

            if (node.Children.Count == 0)
            {
                return;
            }

            if (node.Source != null)
            {
                using (context.CodeWriter.BuildLinePragma(node.Source.Value))
                {
                    var offset = DesignTimeVariable.Length + " = ".Length;
                    context.CodeWriter.WritePadding(offset, node.Source, context);
                    context.CodeWriter.WriteStartAssignment(DesignTimeVariable);

                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                        {
                            context.AddSourceMappingFor(token);
                            context.CodeWriter.Write(token.Content);
                        }
                        else
                        {
                            // There may be something else inside the expression like a Template or another extension node.
                            context.RenderNode(node.Children[i]);
                        }
                    }

                    context.CodeWriter.WriteLine(";");
                }
            }
            else
            {
                context.CodeWriter.WriteStartAssignment(DesignTimeVariable);
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
                context.CodeWriter.WriteLine(";");
            }
        }

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

            IDisposable linePragmaScope = null;
            if (node.Source != null)
            {
                if (!isWhitespaceStatement)
                {
                    linePragmaScope = context.CodeWriter.BuildLinePragma(node.Source.Value);
                }

                context.CodeWriter.WritePadding(0, node.Source.Value, context);
            }
            else if (isWhitespaceStatement)
            {
                // Don't write whitespace if there is no line mapping for it.
                return;
            }

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                {
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

            context.RenderChildren(node);
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

            context.RenderChildren(node);
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

            if (node.Children.Count == 0)
            {
                return;
            }

            var firstChild = node.Children[0];
            if (firstChild.Source != null)
            {
                using (context.CodeWriter.BuildLinePragma(firstChild.Source.Value))
                {
                    var offset = DesignTimeVariable.Length + " = ".Length;
                    context.CodeWriter.WritePadding(offset, firstChild.Source, context);
                    context.CodeWriter.WriteStartAssignment(DesignTimeVariable);

                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                        {
                            context.AddSourceMappingFor(token);
                            context.CodeWriter.Write(token.Content);
                        }
                        else
                        {
                            // There may be something else inside the expression like a Template or another extension node.
                            context.RenderNode(node.Children[i]);
                        }
                    }

                    context.CodeWriter.WriteLine(";");
                }
            }
            else
            {
                context.CodeWriter.WriteStartAssignment(DesignTimeVariable);
                for (var i = 0; i < node.Children.Count; i++)
                {
                    if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                    {
                        if (token.Source != null)
                        {
                            context.AddSourceMappingFor(token);
                        }

                        context.CodeWriter.Write(token.Content);
                    }
                    else
                    {
                        // There may be something else inside the expression like a Template or another extension node.
                        context.RenderNode(node.Children[i]);
                    }
                }
                context.CodeWriter.WriteLine(";");
            }
        }

        public override void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is IntermediateToken token && token.IsCSharp)
                {
                    IDisposable linePragmaScope = null;
                    var isWhitespaceStatement = string.IsNullOrWhiteSpace(token.Content);

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

                    context.AddSourceMappingFor(token);
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

            // Do nothing
        }

        public override void BeginWriteAttribute(CodeWriter codeWriter, string key)
        {
            codeWriter
                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(BlazorApi.RenderTreeBuilder.AddAttribute)}")
                .Write("-1")
                .WriteParameterSeparator()
                .WriteStringLiteral(key)
                .WriteParameterSeparator();
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

            // Do nothing
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

            // Do nothing
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

            // We need to be aware of the blazor scope-tracking concept in design-time code generation
            // because each component creates a lambda scope for its child content.
            //
            // We're hacking it a bit here by just forcing every component to have an empty lambda
            _scopeStack.OpenScope(node.TagName, isComponent: true);
            _scopeStack.IncrementCurrentScopeChildCount(context);
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

            // For design time we only care about the case where the attribute has c# code. 
            //
            // We also limit component attributes to simple cases. However there is still a lot of complexity
            // to handle here, since there are a few different cases for how an attribute might be structured.
            //
            // This rougly follows the design of the runtime writer for simplicity.
            if (node.AttributeStructure == AttributeStructure.Minimized)
            {
                // Do nothing
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
                    context.CodeWriter.Write(DesignTimeVariable);
                    context.CodeWriter.Write(" = ");
                    context.CodeWriter.Write("new ");
                    context.CodeWriter.Write(node.BoundAttribute.TypeName);
                    context.CodeWriter.Write("(");
                    context.CodeWriter.WriteLine();
                    WriteCSharpToken(context, token);
                    context.CodeWriter.Write(");");
                    context.CodeWriter.WriteLine();
                }
            }
            else if ((cSharpNode = node.Children[0] as CSharpExpressionIntermediateNode) != null)
            {
                // This is the case when an attribute has an explicit C# transition like:
                // <MyComponent Foo="@bar" />
                context.CodeWriter.Write(DesignTimeVariable);
                context.CodeWriter.Write(" = ");
                WriteCSharpToken(context, ((IntermediateToken)cSharpNode.Children[0]));
                context.CodeWriter.Write(";");
                context.CodeWriter.WriteLine();
            }
            else if ((htmlNode = node.Children[0] as HtmlContentIntermediateNode) != null)
            {
                // Do nothing
            }
            else if (node.Children[0] is IntermediateToken token && token.IsCSharp)
            {
                context.CodeWriter.Write(DesignTimeVariable);
                context.CodeWriter.Write(" = ");
                WriteCSharpToken(context, token);
                context.CodeWriter.Write(";");
                context.CodeWriter.WriteLine();
            }
        }

        private void WriteCSharpToken(CodeRenderingContext context, IntermediateToken token)
        {
            if (string.IsNullOrWhiteSpace(token.Content))
            {
                return;
            }

            if (token.Source?.FilePath == null)
            {
                context.CodeWriter.Write(token.Content);
                return;
            }

            using (context.CodeWriter.BuildLinePragma(token.Source))
            {
                context.CodeWriter.WritePadding(0, token.Source.Value, context);
                context.AddSourceMappingFor(token);
                context.CodeWriter.Write(token.Content);
            }
        }
    }
}
