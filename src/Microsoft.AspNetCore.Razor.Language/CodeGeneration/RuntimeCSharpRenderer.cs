// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class RuntimeCSharpRenderer : PageStructureCSharpRenderer
    {
        public RuntimeCSharpRenderer(RuntimeTarget target, CSharpRenderingContext context)
            : base(target, context)
        {
        }

        public override void VisitChecksum(ChecksumIRNode node)
        {
            if (!string.IsNullOrEmpty(node.Bytes))
            {
                Context.Writer
                .Write("#pragma checksum \"")
                .Write(node.FileName)
                .Write("\" \"")
                .Write(node.Guid)
                .Write("\" \"")
                .Write(node.Bytes)
                .WriteLine("\"");
            }
        }

        public override void VisitHtml(HtmlContentIRNode node)
        {
            // We can't remove this yet, because it's still used recursively in a few places.
            const int MaxStringLiteralLength = 1024;

            var charactersConsumed = 0;

            // Render the string in pieces to avoid Roslyn OOM exceptions at compile time: https://github.com/aspnet/External/issues/54
            while (charactersConsumed < node.Content.Length)
            {
                string textToRender;
                if (node.Content.Length <= MaxStringLiteralLength)
                {
                    textToRender = node.Content;
                }
                else
                {
                    var charactersToSubstring = Math.Min(MaxStringLiteralLength, node.Content.Length - charactersConsumed);
                    textToRender = node.Content.Substring(charactersConsumed, charactersToSubstring);
                }

                Context.Writer
                    .Write(Context.RenderingConventions.StartWriteLiteralMethod)
                    .WriteStringLiteral(textToRender)
                    .WriteEndMethodInvocation();

                charactersConsumed += textToRender.Length;
            }
        }

        public override void VisitCSharpExpression(CSharpExpressionIRNode node)
        {
            // We can't remove this yet, because it's still used recursively in a few places.
            IDisposable linePragmaScope = null;
            if (node.Source != null)
            {
                linePragmaScope = Context.Writer.BuildLinePragma(node.Source.Value);
                var padding = BuildOffsetPadding(Context.RenderingConventions.StartWriteMethod.Length, node.Source.Value, Context);
                Context.Writer.Write(padding);
            }

            Context.Writer.Write(Context.RenderingConventions.StartWriteMethod);

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is RazorIRToken token && token.IsCSharp)
                {
                    Context.Writer.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the expression like a Template or another extension node.
                    Visit(node.Children[i]);
                }
            }

            Context.Writer.WriteEndMethodInvocation();

            linePragmaScope?.Dispose();
        }

        public override void VisitUsingStatement(UsingStatementIRNode node)
        {
            if (node.Source.HasValue)
            {
                using (Context.Writer.BuildLinePragma(node.Source.Value))
                {
                    Context.Writer.WriteUsing(node.Content);
                }
            }
            else
            {
                Context.Writer.WriteUsing(node.Content);
            }
        }

        public override void VisitHtmlAttribute(HtmlAttributeIRNode node)
        {
            var valuePieceCount = node
                .Children
                .Count(child => child is HtmlAttributeValueIRNode || child is CSharpAttributeValueIRNode);
            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var suffixLocation = node.Source.Value.AbsoluteIndex + node.Source.Value.Length - node.Suffix.Length;
            Context.Writer
                .Write(Context.RenderingConventions.StartBeginWriteAttributeMethod)
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

            VisitDefault(node);

            Context.Writer
                .Write(Context.RenderingConventions.StartEndWriteAttributeMethod)
                .WriteEndMethodInvocation();
        }

        public override void VisitHtmlAttributeValue(HtmlAttributeValueIRNode node)
        {
            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
            var valueLength = node.Source.Value.Length;
            Context.Writer
                .Write(Context.RenderingConventions.StartWriteAttributeValueMethod)
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

        public override void VisitCSharpAttributeValue(CSharpAttributeValueIRNode node)
        {
            const string ValueWriterName = "__razor_attribute_value_writer";

            var expressionValue = node.Children.FirstOrDefault() as CSharpExpressionIRNode;
            var linePragma = expressionValue != null ? Context.Writer.BuildLinePragma(node.Source.Value) : null;
            var prefixLocation = node.Source.Value.AbsoluteIndex;
            var valueLocation = node.Source.Value.AbsoluteIndex + node.Prefix.Length;
            var valueLength = node.Source.Value.Length - node.Prefix.Length;
            Context.Writer
                .Write(Context.RenderingConventions.StartWriteAttributeValueMethod)
                .WriteStringLiteral(node.Prefix)
                .WriteParameterSeparator()
                .Write(prefixLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator();

            if (expressionValue != null)
            {
                Debug.Assert(node.Children.Count == 1);

                RenderExpressionInline(expressionValue, Context);
            }
            else
            {
                // Not an expression; need to buffer the result.
                Context.Writer.WriteStartNewObject("Microsoft.AspNetCore.Mvc.Razor.HelperResult" /* ORIGINAL: TemplateTypeName */);

                var initialRenderingConventions = Context.RenderingConventions;
                Context.RenderingConventions = new CSharpRedirectRenderingConventions(ValueWriterName, Context.Writer);
                using (Context.Writer.BuildAsyncLambda(endLine: false, parameterNames: ValueWriterName))
                {
                    VisitDefault(node);
                }
                Context.RenderingConventions = initialRenderingConventions;

                Context.Writer.WriteEndMethodInvocation(false);
            }

            Context.Writer
                .WriteParameterSeparator()
                .Write(valueLocation.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .Write(valueLength.ToString(CultureInfo.InvariantCulture))
                .WriteParameterSeparator()
                .WriteBooleanLiteral(false)
                .WriteEndMethodInvocation();

            linePragma?.Dispose();
        }

        public override void VisitCSharpStatement(CSharpStatementIRNode node)
        {
            // We can't remove this yet, because it's still used recursively in a few places.
            var isWhitespaceStatement = true;
            for (var i = 0; i < node.Children.Count; i++)
            {
                var token = node.Children[i] as RazorIRToken;
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
                linePragmaScope = Context.Writer.BuildLinePragma(node.Source.Value);
                var padding = BuildOffsetPadding(0, node.Source.Value, Context);
                Context.Writer.Write(padding);
            }

            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i] is RazorIRToken token && token.IsCSharp)
                {
                    Context.Writer.Write(token.Content);
                }
                else
                {
                    // There may be something else inside the statement like an extension node.
                    Visit(node.Children[i]);
                }
            }

            if (linePragmaScope == null)
            {
                Context.Writer.WriteLine();
            }

            linePragmaScope?.Dispose();
        }

        public override void VisitAddPreallocatedTagHelperHtmlAttribute(AddPreallocatedTagHelperHtmlAttributeIRNode node)
        {
            Context.Writer
                .WriteStartInstanceMethodInvocation(
                    "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */,
                    "AddHtmlAttribute" /* ORIGINAL: ExecutionContextAddHtmlAttributeMethodName */)
                .Write(node.VariableName)
                .WriteEndMethodInvocation();
        }

        public override void VisitAddTagHelperHtmlAttribute(AddTagHelperHtmlAttributeIRNode node)
        {
            var attributeValueStyleParameter = $"global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.{node.ValueStyle}";
            var isConditionalAttributeValue = node.Children.Any(child => child is CSharpAttributeValueIRNode);

            // All simple text and minimized attributes will be pre-allocated.
            if (isConditionalAttributeValue)
            {
                // Dynamic attribute value should be run through the conditional attribute removal system. It's
                // unbound and contains C#.

                // TagHelper attribute rendering is buffered by default. We do not want to write to the current
                // writer.
                var valuePieceCount = node.Children.Count(
                    child => child is HtmlAttributeValueIRNode || child is CSharpAttributeValueIRNode);

                Context.Writer
                    .WriteStartMethodInvocation("BeginAddHtmlAttributeValues" /* ORIGINAL: BeginAddHtmlAttributeValuesMethodName */)
                    .Write("__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */)
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Name)
                    .WriteParameterSeparator()
                    .Write(valuePieceCount.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(attributeValueStyleParameter)
                    .WriteEndMethodInvocation();

                var initialRenderingConventions = Context.RenderingConventions;
                Context.RenderingConventions = new TagHelperHtmlAttributeRenderingConventions(Context.Writer);
                VisitDefault(node);
                Context.RenderingConventions = initialRenderingConventions;

                Context.Writer
                    .WriteMethodInvocation(
                        "EndAddHtmlAttributeValues" /* ORIGINAL: EndAddHtmlAttributeValuesMethodName */,
                        "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */);
            }
            else
            {
                // This is a data-* attribute which includes C#. Do not perform the conditional attribute removal or
                // other special cases used when IsDynamicAttributeValue(). But the attribute must still be buffered to
                // determine its final value.

                // Attribute value is not plain text, must be buffered to determine its final value.
                Context.Writer.WriteMethodInvocation("BeginWriteTagHelperAttribute" /* ORIGINAL: BeginWriteTagHelperAttributeMethodName */);

                // We're building a writing scope around the provided chunks which captures everything written from the
                // page. Therefore, we do not want to write to any other buffer since we're using the pages buffer to
                // ensure we capture all content that's written, directly or indirectly.
                var initialRenderingConventions = Context.RenderingConventions;
                Context.RenderingConventions = new CSharpRenderingConventions(Context.Writer);
                VisitDefault(node);
                Context.RenderingConventions = initialRenderingConventions;

                Context.Writer
                    .WriteStartAssignment("__tagHelperStringValueBuffer" /* ORIGINAL: StringValueBufferVariableName */)
                    .WriteMethodInvocation("EndWriteTagHelperAttribute" /* ORIGINAL: EndWriteTagHelperAttributeMethodName */)
                    .WriteStartInstanceMethodInvocation(
                        "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */,
                        "AddHtmlAttribute" /* ORIGINAL: ExecutionContextAddHtmlAttributeMethodName */)
                    .WriteStringLiteral(node.Name)
                    .WriteParameterSeparator()
                    .WriteStartMethodInvocation("Html.Raw" /* ORIGINAL: MarkAsHtmlEncodedMethodName */)
                    .Write("__tagHelperStringValueBuffer" /* ORIGINAL: StringValueBufferVariableName */)
                    .WriteEndMethodInvocation(endLine: false)
                    .WriteParameterSeparator()
                    .Write(attributeValueStyleParameter)
                    .WriteEndMethodInvocation();
            }
        }

        public override void VisitSetPreallocatedTagHelperProperty(SetPreallocatedTagHelperPropertyIRNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);
            var propertyValueAccessor = GetTagHelperPropertyAccessor(node.IsIndexerNameMatch, tagHelperVariableName, node.AttributeName, node.Descriptor);
            var attributeValueAccessor = $"{node.VariableName}.Value" /* ORIGINAL: TagHelperAttributeValuePropertyName */;
            Context.Writer
                .WriteStartAssignment(propertyValueAccessor)
                .Write("(string)")
                .Write(attributeValueAccessor)
                .WriteLine(";")
                .WriteStartInstanceMethodInvocation(
                    "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */,
                    "AddTagHelperAttribute" /* ORIGINAL: ExecutionContextAddTagHelperAttributeMethodName */)
                .Write(node.VariableName)
                .WriteEndMethodInvocation();
        }

        public override void VisitSetTagHelperProperty(SetTagHelperPropertyIRNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);
            var tagHelperRenderingContext = Context.TagHelperRenderingContext;
            var propertyName = node.Descriptor.Metadata[ITagHelperBoundAttributeDescriptorBuilder.PropertyNameKey];

            // Ensure that the property we're trying to set has initialized its dictionary bound properties.
            if (node.IsIndexerNameMatch &&
                tagHelperRenderingContext.VerifiedPropertyDictionaries.Add(propertyName))
            {
                // Throw a reasonable Exception at runtime if the dictionary property is null.
                Context.Writer
                    .Write("if (")
                    .Write(tagHelperVariableName)
                    .Write(".")
                    .Write(propertyName)
                    .WriteLine(" == null)");
                using (Context.Writer.BuildScope())
                {
                    // System is in Host.NamespaceImports for all MVC scenarios. No need to generate FullName
                    // of InvalidOperationException type.
                    Context.Writer
                        .Write("throw ")
                        .WriteStartNewObject(nameof(InvalidOperationException))
                        .WriteStartMethodInvocation("InvalidTagHelperIndexerAssignment" /* ORIGINAL: FormatInvalidIndexerAssignmentMethodName */)
                        .WriteStringLiteral(node.AttributeName)
                        .WriteParameterSeparator()
                        .WriteStringLiteral(node.TagHelperTypeName)
                        .WriteParameterSeparator()
                        .WriteStringLiteral(propertyName)
                        .WriteEndMethodInvocation(endLine: false)   // End of method call
                        .WriteEndMethodInvocation();   // End of new expression / throw statement
                }
            }

            var propertyValueAccessor = GetTagHelperPropertyAccessor(node.IsIndexerNameMatch, tagHelperVariableName, node.AttributeName, node.Descriptor);

            string previousValueAccessor;
            if (tagHelperRenderingContext.RenderedBoundAttributes.TryGetValue(node.AttributeName, out previousValueAccessor))
            {
                Context.Writer
                    .WriteStartAssignment(propertyValueAccessor)
                    .Write(previousValueAccessor)
                    .WriteLine(";");

                return;
            }
            else
            {
                tagHelperRenderingContext.RenderedBoundAttributes[node.AttributeName] = propertyValueAccessor;
            }

            if (node.Descriptor.IsStringProperty || (node.IsIndexerNameMatch && node.Descriptor.IsIndexerStringProperty))
            {
                Context.Writer.WriteMethodInvocation("BeginWriteTagHelperAttribute" /* ORIGINAL: BeginWriteTagHelperAttributeMethodName */);

                var initialRenderingConventions = Context.RenderingConventions;
                Context.RenderingConventions = new CSharpLiteralCodeConventions(Context.Writer);
                VisitDefault(node);
                Context.RenderingConventions = initialRenderingConventions;

                Context.Writer
                    .WriteStartAssignment("__tagHelperStringValueBuffer" /* ORIGINAL: StringValueBufferVariableName */)
                    .WriteMethodInvocation("EndWriteTagHelperAttribute" /* ORIGINAL: EndWriteTagHelperAttributeMethodName */)
                    .WriteStartAssignment(propertyValueAccessor)
                    .Write("__tagHelperStringValueBuffer" /* ORIGINAL: StringValueBufferVariableName */)
                    .WriteLine(";");
            }
            else
            {
                using (Context.Writer.BuildLinePragma(node.Source.Value))
                {
                    Context.Writer.WriteStartAssignment(propertyValueAccessor);

                    if (node.Descriptor.IsEnum &&
                        node.Children.Count == 1 &&
                        node.Children.First() is HtmlContentIRNode)
                    {
                        Context.Writer
                            .Write("global::")
                            .Write(node.Descriptor.TypeName)
                            .Write(".");
                    }

                    RenderTagHelperAttributeInline(node, node.Source.Value);

                    Context.Writer.WriteLine(";");
                }
            }

            // We need to inform the context of the attribute value.
            Context.Writer
                .WriteStartInstanceMethodInvocation(
                    "__tagHelperExecutionContext" /* ORIGINAL: ExecutionContextVariableName */,
                    "AddTagHelperAttribute" /* ORIGINAL: ExecutionContextAddTagHelperAttributeMethodName */)
                .WriteStringLiteral(node.AttributeName)
                .WriteParameterSeparator()
                .Write(propertyValueAccessor)
                .WriteParameterSeparator()
                .Write($"global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.{node.ValueStyle}")
                .WriteEndMethodInvocation();
        }

        public override void VisitDeclarePreallocatedTagHelperHtmlAttribute(DeclarePreallocatedTagHelperHtmlAttributeIRNode node)
        {
            Context.Writer
                .Write("private static readonly global::")
                .Write("Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute" /* ORIGINAL: TagHelperAttributeTypeName */)
                .Write(" ")
                .Write(node.VariableName)
                .Write(" = ")
                .WriteStartNewObject("global::" + "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute" /* ORIGINAL: TagHelperAttributeTypeName */)
                .WriteStringLiteral(node.Name);

            if (node.ValueStyle == HtmlAttributeValueStyle.Minimized)
            {
                Context.Writer.WriteEndMethodInvocation();
            }
            else
            {
                Context.Writer
                    .WriteParameterSeparator()
                    .WriteStartNewObject("global::" + "Microsoft.AspNetCore.Html.HtmlString" /* ORIGINAL: EncodedHtmlStringTypeName */)
                    .WriteStringLiteral(node.Value)
                    .WriteEndMethodInvocation(endLine: false)
                    .WriteParameterSeparator()
                    .Write($"global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.{node.ValueStyle}")
                    .WriteEndMethodInvocation();
            }
        }

        public override void VisitDeclarePreallocatedTagHelperAttribute(DeclarePreallocatedTagHelperAttributeIRNode node)
        {
            Context.Writer
                .Write("private static readonly global::")
                .Write("Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute" /* ORIGINAL: TagHelperAttributeTypeName */)
                .Write(" ")
                .Write(node.VariableName)
                .Write(" = ")
                .WriteStartNewObject("global::" + "Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute" /* ORIGINAL: TagHelperAttributeTypeName */)
                .WriteStringLiteral(node.Name)
                .WriteParameterSeparator()
                .WriteStringLiteral(node.Value)
                .WriteParameterSeparator()
                .Write($"global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.{node.ValueStyle}")
                .WriteEndMethodInvocation();
        }

        private void RenderTagHelperAttributeInline(
            RazorIRNode node,
            SourceSpan documentLocation)
        {
            if (node is SetTagHelperPropertyIRNode || node is CSharpExpressionIRNode)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    RenderTagHelperAttributeInline(node.Children[i], documentLocation);
                }
            }
            else if (node is HtmlContentIRNode)
            {
                Context.Writer.Write(((HtmlContentIRNode)node).Content);
            }
            else if (node is RazorIRToken token && token.IsCSharp)
            {
                Context.Writer.Write(token.Content);
            }
            else if (node is CSharpStatementIRNode)
            {
                var error = new RazorError(
                    LegacyResources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
                    new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                    documentLocation.Length);
                Context.Diagnostics.Add(RazorDiagnostic.Create(error));
            }
            else if (node is TemplateIRNode)
            {
                var attributeValueNode = (SetTagHelperPropertyIRNode)node.Parent;
                var expectedTypeName = attributeValueNode.IsIndexerNameMatch ? 
                    attributeValueNode.Descriptor.IndexerTypeName : 
                    attributeValueNode.Descriptor.TypeName;
                var error = new RazorError(
                    LegacyResources.FormatTagHelpers_InlineMarkupBlocks_NotSupported_InAttributes(expectedTypeName),
                    new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                    documentLocation.Length);
                Context.Diagnostics.Add(RazorDiagnostic.Create(error));
            }
        }
    }
}
