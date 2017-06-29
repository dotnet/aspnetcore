// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class RuntimeTagHelperWriter : TagHelperWriter
    {
        public virtual string WriteTagHelperOutputMethod { get; set; } = "Write";

        public string StringValueBufferVariableName { get; set; } = "__tagHelperStringValueBuffer";

        public string ExecutionContextTypeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext";

        public string ExecutionContextVariableName { get; set; } = "__tagHelperExecutionContext";

        public string ExecutionContextAddMethodName { get; set; } = "Add";

        public string ExecutionContextOutputPropertyName { get; set; } = "Output";

        public string ExecutionContextSetOutputContentAsyncMethodName { get; set; } = "SetOutputContentAsync";

        public string ExecutionContextAddHtmlAttributeMethodName { get; set; } = "AddHtmlAttribute";

        public string ExecutionContextAddTagHelperAttributeMethodName { get; set; } = "AddTagHelperAttribute";

        public string RunnerTypeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner";

        public string RunnerVariableName { get; set; } = "__tagHelperRunner";

        public string RunnerRunAsyncMethodName { get; set; } = "RunAsync";

        public string ScopeManagerTypeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager";

        public string ScopeManagerVariableName { get; set; } = "__tagHelperScopeManager";

        public string ScopeManagerBeginMethodName { get; set; } = "Begin";

        public string ScopeManagerEndMethodName { get; set; } = "End";

        public string StartTagHelperWritingScopeMethodName { get; set; } = "StartTagHelperWritingScope";

        public string EndTagHelperWritingScopeMethodName { get; set; } = "EndTagHelperWritingScope";

        public string TagModeTypeName { get; set; } = "global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode";

        public string HtmlAttributeValueStyleTypeName { get; set; } = "global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle";

        public string CreateTagHelperMethodName { get; set; } = "CreateTagHelper";

        public string TagHelperOutputIsContentModifiedPropertyName { get; set; } = "IsContentModified";

        public string BeginAddHtmlAttributeValuesMethodName { get; set; } = "BeginAddHtmlAttributeValues";

        public string EndAddHtmlAttributeValuesMethodName { get; set; } = "EndAddHtmlAttributeValues";

        public string BeginWriteTagHelperAttributeMethodName { get; set; } = "BeginWriteTagHelperAttribute";

        public string EndWriteTagHelperAttributeMethodName { get; set; } = "EndWriteTagHelperAttribute";

        public string MarkAsHtmlEncodedMethodName { get; set; } = "Html.Raw";

        public string FormatInvalidIndexerAssignmentMethodName { get; set; } = "InvalidTagHelperIndexerAssignment";

        public override void WriteDeclareTagHelperFields(CodeRenderingContext context, DeclareTagHelperFieldsIntermediateNode node)
        {
            context.CodeWriter.WriteLine("#line hidden");

            // Need to disable the warning "X is assigned to but never used." for the value buffer since
            // whether it's used depends on how a TagHelper is used.
            context.CodeWriter
                .WriteLine("#pragma warning disable 0414")
                .Write("private ")
                .WriteVariableDeclaration("string", StringValueBufferVariableName, value: null)
                .WriteLine("#pragma warning restore 0414");

            context.CodeWriter
            .Write("private ")
            .WriteVariableDeclaration(
                ExecutionContextTypeName,
                ExecutionContextVariableName,
                value: null);

            context.CodeWriter
            .Write("private ")
            .Write(RunnerTypeName)
            .Write(" ")
            .Write(RunnerVariableName)
            .Write(" = new ")
            .Write(RunnerTypeName)
            .WriteLine("();");

            var backedScopeManageVariableName = "__backed" + ScopeManagerVariableName;
            context.CodeWriter
                .Write("private ")
                .WriteVariableDeclaration(
                    ScopeManagerTypeName,
                    backedScopeManageVariableName,
                    value: null);

            context.CodeWriter
            .Write("private ")
            .Write(ScopeManagerTypeName)
            .Write(" ")
            .WriteLine(ScopeManagerVariableName);

            using (context.CodeWriter.BuildScope())
            {
                context.CodeWriter.WriteLine("get");
                using (context.CodeWriter.BuildScope())
                {
                    context.CodeWriter
                        .Write("if (")
                        .Write(backedScopeManageVariableName)
                        .WriteLine(" == null)");

                    using (context.CodeWriter.BuildScope())
                    {
                        context.CodeWriter
                            .WriteStartAssignment(backedScopeManageVariableName)
                            .WriteStartNewObject(ScopeManagerTypeName)
                            .Write(StartTagHelperWritingScopeMethodName)
                            .WriteParameterSeparator()
                            .Write(EndTagHelperWritingScopeMethodName)
                            .WriteEndMethodInvocation();
                    }

                    context.CodeWriter
                        .Write("return ")
                        .Write(backedScopeManageVariableName)
                        .WriteLine(";");
                }
            }

            foreach (var tagHelperTypeName in node.UsedTagHelperTypeNames)
            {
                var tagHelperVariableName = GetTagHelperVariableName(tagHelperTypeName);
                context.CodeWriter
                    .Write("private global::")
                    .WriteVariableDeclaration(
                        tagHelperTypeName,
                        tagHelperVariableName,
                        value: null);
            }
        }

        public override void WriteTagHelper(CodeRenderingContext context, TagHelperIntermediateNode node)
        {
            context.RenderChildren(node);

            // Execute tag helpers
            context.CodeWriter
                .Write("await ")
                .WriteStartInstanceMethodInvocation(
                    RunnerVariableName,
                    RunnerRunAsyncMethodName)
                .Write(ExecutionContextVariableName)
                .WriteEndMethodInvocation();

            var tagHelperOutputAccessor = $"{ExecutionContextVariableName}.{ExecutionContextOutputPropertyName}";

            context.CodeWriter
                .Write("if (!")
                .Write(tagHelperOutputAccessor)
                .Write(".")
                .Write(TagHelperOutputIsContentModifiedPropertyName)
                .WriteLine(")");

            using (context.CodeWriter.BuildScope())
            {
                context.CodeWriter
                    .Write("await ")
                    .WriteInstanceMethodInvocation(
                        ExecutionContextVariableName,
                        ExecutionContextSetOutputContentAsyncMethodName);
            }

            context.CodeWriter
                .WriteStartMethodInvocation(WriteTagHelperOutputMethod)
                .Write(tagHelperOutputAccessor)
                .WriteEndMethodInvocation()
                .WriteStartAssignment(ExecutionContextVariableName)
                .WriteInstanceMethodInvocation(
                    ScopeManagerVariableName,
                    ScopeManagerEndMethodName);
        }

        public override void WriteTagHelperBody(CodeRenderingContext context, TagHelperBodyIntermediateNode node)
        {
            // Call into the tag helper scope manager to start a new tag helper scope.
            // Also capture the value as the current execution context.
            context.CodeWriter
                .WriteStartAssignment(ExecutionContextVariableName)
                .WriteStartInstanceMethodInvocation(
                    ScopeManagerVariableName,
                    ScopeManagerBeginMethodName);

            var uniqueId = context.Items[CodeRenderingContext.SuppressUniqueIds]?.ToString();
            if (uniqueId == null)
            {
                uniqueId = Guid.NewGuid().ToString("N");
            }

            // Assign a unique ID for this instance of the source HTML tag. This must be unique
            // per call site, e.g. if the tag is on the view twice, there should be two IDs.
            context.CodeWriter.WriteStringLiteral(context.TagHelperRenderingContext.TagName)
                .WriteParameterSeparator()
                .Write(TagModeTypeName)
                .Write(".")
                .Write(context.TagHelperRenderingContext.TagMode.ToString())
                .WriteParameterSeparator()
                .WriteStringLiteral(uniqueId)
                .WriteParameterSeparator();

            // We remove and redirect writers so TagHelper authors can retrieve content.
            using (context.Push(new RuntimeNodeWriter()))
            using (context.Push(new RuntimeTagHelperWriter()))
            {
                using (context.CodeWriter.BuildAsyncLambda())
                {
                    context.RenderChildren(node);
                }
            }

            context.CodeWriter.WriteEndMethodInvocation();
        }

        public override void WriteCreateTagHelper(CodeRenderingContext context, CreateTagHelperIntermediateNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);

            context.CodeWriter
                .WriteStartAssignment(tagHelperVariableName)
                .Write(CreateTagHelperMethodName)
                .WriteLine($"<global::{node.TagHelperTypeName}>();");

            context.CodeWriter.WriteInstanceMethodInvocation(
                ExecutionContextVariableName,
                ExecutionContextAddMethodName,
                tagHelperVariableName);
        }

        public override void WriteAddTagHelperHtmlAttribute(CodeRenderingContext context, AddTagHelperHtmlAttributeIntermediateNode node)
        {
            var attributeValueStyleParameter = $"{HtmlAttributeValueStyleTypeName}.{node.AttributeStructure}";
            var isConditionalAttributeValue = node.Children.Any(
                child => child is CSharpExpressionAttributeValueIntermediateNode || child is CSharpCodeAttributeValueIntermediateNode);

            // All simple text and minimized attributes will be pre-allocated.
            if (isConditionalAttributeValue)
            {
                // Dynamic attribute value should be run through the conditional attribute removal system. It's
                // unbound and contains C#.

                // TagHelper attribute rendering is buffered by default. We do not want to write to the current
                // writer.
                var valuePieceCount = node.Children.Count(
                    child =>
                        child is HtmlAttributeValueIntermediateNode ||
                        child is CSharpExpressionAttributeValueIntermediateNode ||
                        child is CSharpCodeAttributeValueIntermediateNode ||
                        child is ExtensionIntermediateNode);

                context.CodeWriter
                    .WriteStartMethodInvocation(BeginAddHtmlAttributeValuesMethodName)
                    .Write(ExecutionContextVariableName)
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Name)
                    .WriteParameterSeparator()
                    .Write(valuePieceCount.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(attributeValueStyleParameter)
                    .WriteEndMethodInvocation();

                using (context.Push(new TagHelperHtmlAttributeRuntimeNodeWriter()))
                {
                    context.RenderChildren(node);
                }

                context.CodeWriter
                    .WriteMethodInvocation(
                        EndAddHtmlAttributeValuesMethodName,
                        ExecutionContextVariableName);
            }
            else
            {
                // This is a data-* attribute which includes C#. Do not perform the conditional attribute removal or
                // other special cases used when IsDynamicAttributeValue(). But the attribute must still be buffered to
                // determine its final value.

                // Attribute value is not plain text, must be buffered to determine its final value.
                context.CodeWriter.WriteMethodInvocation(BeginWriteTagHelperAttributeMethodName);

                // We're building a writing scope around the provided chunks which captures everything written from the
                // page. Therefore, we do not want to write to any other buffer since we're using the pages buffer to
                // ensure we capture all content that's written, directly or indirectly.
                using (context.Push(new RuntimeNodeWriter()))
                using (context.Push(new RuntimeTagHelperWriter()))
                {
                    context.RenderChildren(node);
                }

                context.CodeWriter
                    .WriteStartAssignment(StringValueBufferVariableName)
                    .WriteMethodInvocation(EndWriteTagHelperAttributeMethodName)
                    .WriteStartInstanceMethodInvocation(
                        ExecutionContextVariableName,
                        ExecutionContextAddHtmlAttributeMethodName)
                    .WriteStringLiteral(node.Name)
                    .WriteParameterSeparator()
                    .WriteStartMethodInvocation(MarkAsHtmlEncodedMethodName)
                    .Write(StringValueBufferVariableName)
                    .WriteEndMethodInvocation(endLine: false)
                    .WriteParameterSeparator()
                    .Write(attributeValueStyleParameter)
                    .WriteEndMethodInvocation();
            }
        }

        public override void WriteSetTagHelperProperty(CodeRenderingContext context, SetTagHelperPropertyIntermediateNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);
            var tagHelperRenderingContext = context.TagHelperRenderingContext;
            var propertyName = node.Descriptor.GetPropertyName();

            // Ensure that the property we're trying to set has initialized its dictionary bound properties.
            if (node.IsIndexerNameMatch &&
                tagHelperRenderingContext.VerifiedPropertyDictionaries.Add($"{node.TagHelperTypeName}.{propertyName}"))
            {
                // Throw a reasonable Exception at runtime if the dictionary property is null.
                context.CodeWriter
                    .Write("if (")
                    .Write(tagHelperVariableName)
                    .Write(".")
                    .Write(propertyName)
                    .WriteLine(" == null)");
                using (context.CodeWriter.BuildScope())
                {
                    // System is in Host.NamespaceImports for all MVC scenarios. No need to generate FullName
                    // of InvalidOperationException type.
                    context.CodeWriter
                        .Write("throw ")
                        .WriteStartNewObject(nameof(InvalidOperationException))
                        .WriteStartMethodInvocation(FormatInvalidIndexerAssignmentMethodName)
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

            if (tagHelperRenderingContext.RenderedBoundAttributes.TryGetValue(node.AttributeName, out var previousValueAccessor))
            {
                context.CodeWriter
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
                context.CodeWriter.WriteMethodInvocation(BeginWriteTagHelperAttributeMethodName);

                using (context.Push(new LiteralRuntimeNodeWriter()))
                {
                    context.RenderChildren(node);
                }

                context.CodeWriter
                    .WriteStartAssignment(StringValueBufferVariableName)
                    .WriteMethodInvocation(EndWriteTagHelperAttributeMethodName)
                    .WriteStartAssignment(propertyValueAccessor)
                    .Write(StringValueBufferVariableName)
                    .WriteLine(";");
            }
            else
            {
                using (context.CodeWriter.BuildLinePragma(node.Source.Value))
                {
                    context.CodeWriter.WriteStartAssignment(propertyValueAccessor);

                    if (node.Descriptor.IsEnum &&
                        node.Children.Count == 1 &&
                        node.Children.First() is IntermediateToken token &&
                        token.IsCSharp)
                    {
                        context.CodeWriter
                            .Write("global::")
                            .Write(node.Descriptor.TypeName)
                            .Write(".");
                    }

                    RenderTagHelperAttributeInline(context, node, node.Source.Value);

                    context.CodeWriter.WriteLine(";");
                }
            }

            // We need to inform the context of the attribute value.
            context.CodeWriter
                .WriteStartInstanceMethodInvocation(
                    ExecutionContextVariableName,
                    ExecutionContextAddTagHelperAttributeMethodName)
                .WriteStringLiteral(node.AttributeName)
                .WriteParameterSeparator()
                .Write(propertyValueAccessor)
                .WriteParameterSeparator()
                .Write($"global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.{node.AttributeStructure}")
                .WriteEndMethodInvocation();
        }

        private void RenderTagHelperAttributeInline(
            CodeRenderingContext context,
            SetTagHelperPropertyIntermediateNode property,
            SourceSpan documentLocation)
        {
            for (var i = 0; i < property.Children.Count; i++)
            {
                RenderTagHelperAttributeInline(context, property, property.Children[i], documentLocation);
            }
        }

        private void RenderTagHelperAttributeInline(
            CodeRenderingContext context,
            SetTagHelperPropertyIntermediateNode property,
            IntermediateNode node,
            SourceSpan documentLocation)
        {
            if (node is CSharpExpressionIntermediateNode || node is HtmlContentIntermediateNode)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    RenderTagHelperAttributeInline(context, property, node.Children[i], documentLocation);
                }
            }
            else if (node is IntermediateToken token)
            {
                context.CodeWriter.Write(token.Content);
            }
            else if (node is CSharpCodeIntermediateNode)
            {
                var error = new RazorError(
                    LegacyResources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
                    new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                    documentLocation.Length);
                context.Diagnostics.Add(RazorDiagnostic.Create(error));
            }
            else if (node is TemplateIntermediateNode)
            {
                var expectedTypeName = property.IsIndexerNameMatch ? property.Descriptor.IndexerTypeName : property.Descriptor.TypeName;
                var error = new RazorError(
                    LegacyResources.FormatTagHelpers_InlineMarkupBlocks_NotSupported_InAttributes(expectedTypeName),
                    new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                    documentLocation.Length);
                context.Diagnostics.Add(RazorDiagnostic.Create(error));
            }
        }

        protected static string GetTagHelperPropertyAccessor(
            bool isIndexerNameMatch,
            string tagHelperVariableName,
            string attributeName,
            BoundAttributeDescriptor descriptor)
        {
            var propertyAccessor = $"{tagHelperVariableName}.{descriptor.GetPropertyName()}";

            if (isIndexerNameMatch)
            {
                var dictionaryKey = attributeName.Substring(descriptor.IndexerNamePrefix.Length);
                propertyAccessor += $"[\"{dictionaryKey}\"]";
            }

            return propertyAccessor;
        }

        private static string GetTagHelperVariableName(string tagHelperTypeName) => "__" + tagHelperTypeName.Replace('.', '_');
    }
}
