// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Blazor.Shared;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    // Based on the DesignTimeNodeWriter from Razor repo.
    internal class BlazorDesignTimeNodeWriter : BlazorNodeWriter
    {
        private readonly ScopeStack _scopeStack = new ScopeStack();

        private readonly static string DesignTimeVariable = "__o";

        public override void WriteHtmlBlock(CodeRenderingContext context, HtmlBlockIntermediateNode node)
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

            context.RenderChildren(node);
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

            // Do nothing, this can't contain code.
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
            if (codeWriter == null)
            {
                throw new ArgumentNullException(nameof(codeWriter));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            codeWriter
                .WriteStartMethodInvocation($"{_scopeStack.BuilderVarName}.{nameof(BlazorApi.RenderTreeBuilder.AddAttribute)}")
                .Write("-1")
                .WriteParameterSeparator()
                .WriteStringLiteral(key)
                .WriteParameterSeparator();
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

            foreach (var typeArgument in node.TypeArguments)
            {
                context.RenderNode(typeArgument);
            }

            foreach (var attribute in node.Attributes)
            {
                context.RenderNode(attribute);
            }

            if (node.ChildContents.Any())
            {
                foreach (var childContent in node.ChildContents)
                {
                    context.RenderNode(childContent);
                }
            }
            else
            {
                // We eliminate 'empty' child content when building the tree so that usage like
                // '<MyComponent>\r\n</MyComponent>' doesn't create a child content.
                //
                // Consider what would happen if the user's cursor was inside the element. At
                // design -time we want to render an empty lambda to provide proper scoping
                // for any code that the user types.
                context.RenderNode(new ComponentChildContentIntermediateNode()
                {
                    TypeName = BlazorApi.RenderFragment.FullTypeName,
                });
            }

            foreach (var capture in node.Captures)
            {
                context.RenderNode(capture);
            }
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
            // This roughly follows the design of the runtime writer for simplicity.
            if (node.AttributeStructure == AttributeStructure.Minimized)
            {
                // Do nothing
            }
            else if (node.Children.Count > 1)
            {
                // We don't expect this to happen, we just want to know if it can.
                throw new InvalidOperationException("Attribute nodes should either be minimized or a single type of content." + string.Join(", ", node.Children));
            }
            else if (node.Children.Count == 1 && node.Children[0] is HtmlContentIntermediateNode)
            {
                // Do nothing
            }
            else
            {
                // There are a few different forms that could be used to contain all of the tokens, but we don't really care
                // exactly what it looks like - we just want all of the content.
                //
                // This can include an empty list in some cases like the following (sic):
                //      <MyComponent Value="
                //
                // Or a CSharpExpressionIntermediateNode when the attribute has an explicit transition like:
                //      <MyComponent Value="@value" />
                //
                // Of a list of tokens directly in the attribute.
                var tokens = GetCSharpTokens(node);

                if ((node.BoundAttribute?.IsDelegateProperty() ?? false) ||
                    (node.BoundAttribute?.IsChildContentProperty() ?? false))
                {
                    // We always surround the expression with the delegate constructor. This makes type
                    // inference inside lambdas, and method group conversion do the right thing.
                    context.CodeWriter.Write(DesignTimeVariable);
                    context.CodeWriter.Write(" = ");
                    context.CodeWriter.Write("new ");
                    context.CodeWriter.Write(node.TypeName);
                    context.CodeWriter.Write("(");
                    context.CodeWriter.WriteLine();

                    for (var i = 0; i < tokens.Count; i++)
                    {
                        WriteCSharpToken(context, tokens[i]);
                    }

                    context.CodeWriter.Write(");");
                    context.CodeWriter.WriteLine();
                }
                else
                {
                    // This is the case when an attribute contains C# code
                    context.CodeWriter.Write(DesignTimeVariable);
                    context.CodeWriter.Write(" = ");

                    // If we have a parameter type, then add a type check.
                    if (NeedsTypeCheck(node))
                    {
                        context.CodeWriter.Write(BlazorApi.RuntimeHelpers.TypeCheck);
                        context.CodeWriter.Write("<");
                        context.CodeWriter.Write(node.TypeName);
                        context.CodeWriter.Write(">");
                        context.CodeWriter.Write("(");
                    }

                    for (var i = 0; i < tokens.Count; i++)
                    {
                        WriteCSharpToken(context, tokens[i]);
                    }

                    if (NeedsTypeCheck(node))
                    {
                        context.CodeWriter.Write(")");
                    }

                    context.CodeWriter.Write(";");
                    context.CodeWriter.WriteLine();
                }
            }

            bool NeedsTypeCheck(ComponentAttributeExtensionNode n)
            {
                return n.BoundAttribute != null && !n.BoundAttribute.IsWeaklyTyped();
            }

            IReadOnlyList<IntermediateToken> GetCSharpTokens(ComponentAttributeExtensionNode attribute)
            {
                // We generally expect all children to be CSharp, this is here just in case.
                return attribute.FindDescendantNodes<IntermediateToken>().Where(t => t.IsCSharp).ToArray();
            }
        }

        public override void WriteComponentChildContent(CodeRenderingContext context, ComponentChildContentIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            _scopeStack.OpenComponentScope(
                context,
                node.AttributeName,
                node.TypeName,
                node.IsParameterized ? node.ParameterName : null);
            for (var i = 0; i < node.Children.Count; i++)
            {
                context.RenderNode(node.Children[i]);
            }
            _scopeStack.CloseScope(context);
        }

        public override void WriteComponentTypeArgument(CodeRenderingContext context, ComponentTypeArgumentExtensionNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // At design type we want write the equivalent of:
            //
            // __o = typeof(TItem);
            context.CodeWriter.Write(DesignTimeVariable);
            context.CodeWriter.Write(" = ");
            context.CodeWriter.Write("typeof(");

            var tokens = GetCSharpTokens(node);
            for (var i = 0; i < tokens.Count; i++)
            {
                WriteCSharpToken(context, tokens[i]);
            }

            context.CodeWriter.Write(");");
            context.CodeWriter.WriteLine();

            IReadOnlyList<IntermediateToken> GetCSharpTokens(ComponentTypeArgumentExtensionNode arg)
            {
                // We generally expect all children to be CSharp, this is here just in case.
                return arg.FindDescendantNodes<IntermediateToken>().Where(t => t.IsCSharp).ToArray();
            }
        }

        public override void WriteTemplate(CodeRenderingContext context, TemplateIntermediateNode node)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            _scopeStack.OpenTemplateScope(context);
            context.RenderChildren(node);
            _scopeStack.CloseScope(context);
        }

        public override void WriteReferenceCapture(CodeRenderingContext context, RefExtensionNode refNode)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (refNode == null)
            {
                throw new ArgumentNullException(nameof(refNode));
            }

            // The runtime node writer moves the call elsewhere. At design time we
            // just want sufficiently similar code that any unknown-identifier or type
            // errors will be equivalent
            var captureTypeName = refNode.IsComponentCapture
                ? refNode.ComponentCaptureTypeName
                : BlazorApi.ElementRef.FullTypeName;
            WriteCSharpCode(context, new CSharpCodeIntermediateNode
            {
                Source = refNode.Source,
                Children =
                {
                    refNode.IdentifierToken,
                    new IntermediateToken
                    {
                        Kind = TokenKind.CSharp,
                        Content = $" = default({captureTypeName});"
                    }
                }
            });
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
