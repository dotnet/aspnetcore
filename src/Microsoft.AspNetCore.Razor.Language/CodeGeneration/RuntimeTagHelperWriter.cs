// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class RuntimeTagHelperWriter : TagHelperWriter
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

        public string CreateTagHelperMethodName { get; set; } = "CreateTagHelper";

        public string TagHelperOutputIsContentModifiedPropertyName { get; set; } = "IsContentModified";

        public string BeginAddHtmlAttributeValuesMethodName { get; set; } = "BeginAddHtmlAttributeValues";

        public string EndAddHtmlAttributeValuesMethodName { get; set; } = "EndAddHtmlAttributeValues";

        public string BeginWriteTagHelperAttributeMethodName { get; set; } = "BeginWriteTagHelperAttribute";

        public string EndWriteTagHelperAttributeMethodName { get; set; } = "EndWriteTagHelperAttribute";

        public string MarkAsHtmlEncodedMethodName { get; set; } = "Html.Raw";

        public string FormatInvalidIndexerAssignmentMethodName { get; set; } = "InvalidTagHelperIndexerAssignment";

        public override void WriteDeclareTagHelperFields(CSharpRenderingContext context, DeclareTagHelperFieldsIRNode node)
        {
            context.Writer.WriteLineHiddenDirective();

            // Need to disable the warning "X is assigned to but never used." for the value buffer since
            // whether it's used depends on how a TagHelper is used.
            context.Writer
                .WritePragma("warning disable 0414")
                .Write("private ")
                .WriteVariableDeclaration("string", StringValueBufferVariableName, value: null)
                .WritePragma("warning restore 0414");

            context.Writer
            .Write("private ")
            .WriteVariableDeclaration(
                ExecutionContextTypeName,
                ExecutionContextVariableName,
                value: null);

            context.Writer
            .Write("private ")
            .Write(RunnerTypeName)
            .Write(" ")
            .Write(RunnerVariableName)
            .Write(" = new ")
            .Write(RunnerTypeName)
            .WriteLine("();");

            var backedScopeManageVariableName = "__backed" + ScopeManagerVariableName;
            context.Writer
                .Write("private ")
                .WriteVariableDeclaration(
                    ScopeManagerTypeName,
                    backedScopeManageVariableName,
                    value: null);

            context.Writer
            .Write("private ")
            .Write(ScopeManagerTypeName)
            .Write(" ")
            .WriteLine(ScopeManagerVariableName);

            using (context.Writer.BuildScope())
            {
                context.Writer.WriteLine("get");
                using (context.Writer.BuildScope())
                {
                    context.Writer
                        .Write("if (")
                        .Write(backedScopeManageVariableName)
                        .WriteLine(" == null)");

                    using (context.Writer.BuildScope())
                    {
                        context.Writer
                            .WriteStartAssignment(backedScopeManageVariableName)
                            .WriteStartNewObject(ScopeManagerTypeName)
                            .Write(StartTagHelperWritingScopeMethodName)
                            .WriteParameterSeparator()
                            .Write(EndTagHelperWritingScopeMethodName)
                            .WriteEndMethodInvocation();
                    }

                    context.Writer.WriteReturn(backedScopeManageVariableName);
                }
            }

            foreach (var tagHelperTypeName in node.UsedTagHelperTypeNames)
            {
                var tagHelperVariableName = GetTagHelperVariableName(tagHelperTypeName);
                context.Writer
                    .Write("private global::")
                    .WriteVariableDeclaration(
                        tagHelperTypeName,
                        tagHelperVariableName,
                        value: null);
            }
        }

        public override void WriteAddTagHelperHtmlAttribute(CSharpRenderingContext context, AddTagHelperHtmlAttributeIRNode node)
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

                context.Writer
                    .WriteStartMethodInvocation(BeginAddHtmlAttributeValuesMethodName)
                    .Write(ExecutionContextVariableName)
                    .WriteParameterSeparator()
                    .WriteStringLiteral(node.Name)
                    .WriteParameterSeparator()
                    .Write(valuePieceCount.ToString(CultureInfo.InvariantCulture))
                    .WriteParameterSeparator()
                    .Write(attributeValueStyleParameter)
                    .WriteEndMethodInvocation();

                using (context.Push(new TagHelperHtmlAttributeRuntimeBasicWriter()))
                {
                    context.RenderChildren(node);
                }

                context.Writer
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
                context.Writer.WriteMethodInvocation(BeginWriteTagHelperAttributeMethodName);

                // We're building a writing scope around the provided chunks which captures everything written from the
                // page. Therefore, we do not want to write to any other buffer since we're using the pages buffer to
                // ensure we capture all content that's written, directly or indirectly.
                using (context.Push(new RuntimeBasicWriter()))
                using (context.Push(new RuntimeTagHelperWriter()))
                {
                    context.RenderChildren(node);
                }

                context.Writer
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

        public override void WriteCreateTagHelper(CSharpRenderingContext context, CreateTagHelperIRNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);

            context.Writer
                .WriteStartAssignment(tagHelperVariableName)
                .WriteStartMethodInvocation(
                     CreateTagHelperMethodName,
                    "global::" + node.TagHelperTypeName)
                .WriteEndMethodInvocation();

            context.Writer.WriteInstanceMethodInvocation(
                ExecutionContextVariableName,
                ExecutionContextAddMethodName,
                tagHelperVariableName);
        }

        public override void WriteExecuteTagHelpers(CSharpRenderingContext context, ExecuteTagHelpersIRNode node)
        {
            context.Writer
                .Write("await ")
                .WriteStartInstanceMethodInvocation(
                    RunnerVariableName,
                    RunnerRunAsyncMethodName)
                .Write(ExecutionContextVariableName)
                .WriteEndMethodInvocation();

            var tagHelperOutputAccessor = $"{ExecutionContextVariableName}.{ExecutionContextOutputPropertyName}";

            context.Writer
                .Write("if (!")
                .Write(tagHelperOutputAccessor)
                .Write(".")
                .Write(TagHelperOutputIsContentModifiedPropertyName)
                .WriteLine(")");

            using (context.Writer.BuildScope())
            {
                context.Writer
                    .Write("await ")
                    .WriteInstanceMethodInvocation(
                        ExecutionContextVariableName,
                        ExecutionContextSetOutputContentAsyncMethodName);
            }

            context.Writer
                .WriteStartMethodInvocation(WriteTagHelperOutputMethod)
                .Write(tagHelperOutputAccessor)
                .WriteEndMethodInvocation()
                .WriteStartAssignment(ExecutionContextVariableName)
                .WriteInstanceMethodInvocation(
                    ScopeManagerVariableName,
                    ScopeManagerEndMethodName);
        }

        public override void WriteInitializeTagHelperStructure(CSharpRenderingContext context, InitializeTagHelperStructureIRNode node)
        {
            // Call into the tag helper scope manager to start a new tag helper scope.
            // Also capture the value as the current execution context.
            context.Writer
                .WriteStartAssignment(ExecutionContextVariableName)
                .WriteStartInstanceMethodInvocation(
                    ScopeManagerVariableName,
                    ScopeManagerBeginMethodName);

            // Assign a unique ID for this instance of the source HTML tag. This must be unique
            // per call site, e.g. if the tag is on the view twice, there should be two IDs.
            context.Writer.WriteStringLiteral(node.TagName)
                .WriteParameterSeparator()
                .Write(TagModeTypeName)
                .Write(".")
                .Write(node.TagMode.ToString())
                .WriteParameterSeparator()
                .WriteStringLiteral(context.IdGenerator())
                .WriteParameterSeparator();

            // We remove and redirect writers so TagHelper authors can retrieve content.
            using (context.Push(new RuntimeBasicWriter()))
            using (context.Push(new RuntimeTagHelperWriter()))
            {
                using (context.Writer.BuildAsyncLambda(endLine: false))
                {
                    context.RenderChildren(node);
                }
            }

            context.Writer.WriteEndMethodInvocation();
        }

        public override void WriteSetTagHelperProperty(CSharpRenderingContext context, SetTagHelperPropertyIRNode node)
        {
            var tagHelperVariableName = GetTagHelperVariableName(node.TagHelperTypeName);
            var tagHelperRenderingContext = context.TagHelperRenderingContext;
            var propertyName = node.Descriptor.Metadata[ITagHelperBoundAttributeDescriptorBuilder.PropertyNameKey];

            // Ensure that the property we're trying to set has initialized its dictionary bound properties.
            if (node.IsIndexerNameMatch &&
                tagHelperRenderingContext.VerifiedPropertyDictionaries.Add(propertyName))
            {
                // Throw a reasonable Exception at runtime if the dictionary property is null.
                context.Writer
                    .Write("if (")
                    .Write(tagHelperVariableName)
                    .Write(".")
                    .Write(propertyName)
                    .WriteLine(" == null)");
                using (context.Writer.BuildScope())
                {
                    // System is in Host.NamespaceImports for all MVC scenarios. No need to generate FullName
                    // of InvalidOperationException type.
                    context.Writer
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
                context.Writer
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
                context.Writer.WriteMethodInvocation(BeginWriteTagHelperAttributeMethodName);

                using (context.Push(new LiteralRuntimeBasicWriter()))
                {
                    context.RenderChildren(node);
                }

                context.Writer
                    .WriteStartAssignment(StringValueBufferVariableName)
                    .WriteMethodInvocation(EndWriteTagHelperAttributeMethodName)
                    .WriteStartAssignment(propertyValueAccessor)
                    .Write(StringValueBufferVariableName)
                    .WriteLine(";");
            }
            else
            {
                using (context.Writer.BuildLinePragma(node.Source.Value))
                {
                    context.Writer.WriteStartAssignment(propertyValueAccessor);

                    if (node.Descriptor.IsEnum &&
                        node.Children.Count == 1 &&
                        node.Children.First() is HtmlContentIRNode)
                    {
                        context.Writer
                            .Write("global::")
                            .Write(node.Descriptor.TypeName)
                            .Write(".");
                    }

                    RenderTagHelperAttributeInline(context, node, node.Source.Value);

                    context.Writer.WriteLine(";");
                }
            }

            // We need to inform the context of the attribute value.
            context.Writer
                .WriteStartInstanceMethodInvocation(
                    ExecutionContextVariableName,
                    ExecutionContextAddTagHelperAttributeMethodName)
                .WriteStringLiteral(node.AttributeName)
                .WriteParameterSeparator()
                .Write(propertyValueAccessor)
                .WriteParameterSeparator()
                .Write($"global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.{node.ValueStyle}")
                .WriteEndMethodInvocation();
        }

        private void RenderTagHelperAttributeInline(
            CSharpRenderingContext context,
            RazorIRNode node,
            SourceSpan documentLocation)
        {
            if (node is SetTagHelperPropertyIRNode || node is CSharpExpressionIRNode || node is HtmlContentIRNode)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    RenderTagHelperAttributeInline(context, node.Children[i], documentLocation);
                }
            }
            else if (node is RazorIRToken token)
            {
                context.Writer.Write(token.Content);
            }
            else if (node is CSharpStatementIRNode)
            {
                var error = new RazorError(
                    LegacyResources.TagHelpers_CodeBlocks_NotSupported_InAttributes,
                    new SourceLocation(documentLocation.AbsoluteIndex, documentLocation.CharacterIndex, documentLocation.Length),
                    documentLocation.Length);
                context.Diagnostics.Add(RazorDiagnostic.Create(error));
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
                context.Diagnostics.Add(RazorDiagnostic.Create(error));
            }
        }

        protected static string GetTagHelperPropertyAccessor(
            bool isIndexerNameMatch,
            string tagHelperVariableName,
            string attributeName,
            BoundAttributeDescriptor descriptor)
        {
            var propertyAccessor = $"{tagHelperVariableName}.{descriptor.Metadata[ITagHelperBoundAttributeDescriptorBuilder.PropertyNameKey]}";

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
