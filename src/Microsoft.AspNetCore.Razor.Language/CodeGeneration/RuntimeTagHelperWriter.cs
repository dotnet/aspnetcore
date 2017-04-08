// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class RuntimeTagHelperWriter : TagHelperWriter
    {
        public string StringValueBufferVariableName { get; set; } = "__tagHelperStringValueBuffer";

        public string ExecutionContextTypeName { get; set; } = "global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext";

        public string ExecutionContextVariableName { get; set; } = "__tagHelperExecutionContext";

        public string ExecutionContextAddMethodName { get; set; } = "Add";

        public string ExecutionContextOutputPropertyName { get; set; } = "Output";

        public string ExecutionContextSetOutputContentAsyncMethodName { get; set; } = "SetOutputContentAsync";

        public string ExecutionContextAddHtmlAttributeMethodName { get; set; } = "AddHtmlAttribute";

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

        public string WriteTagHelperOutputMethod { get; set; } = "Write";

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

                // This can be removed once all the tag helper nodes are moved out of the renderers.
                var initialRenderingConventions = context.RenderingConventions;
                context.RenderingConventions = new TagHelperHtmlAttributeRenderingConventions(context.Writer);
                using (context.Push(new TagHelperHtmlAttributeRuntimeBasicWriter()))
                {
                    context.RenderChildren(node);
                }
                context.RenderingConventions = initialRenderingConventions;

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
                // This can be removed once all the tag helper nodes are moved out of the renderers.
                var initialRenderingConventions = context.RenderingConventions;
                context.RenderingConventions = new CSharpRenderingConventions(context.Writer);
                using (context.Push(new RuntimeBasicWriter()))
                using (context.Push(new RuntimeTagHelperWriter()))
                {
                    context.RenderChildren(node);
                }
                context.RenderingConventions = initialRenderingConventions;

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
            // This can be removed once all the tag helper nodes are moved out of the renderers.
            var initialRenderingConventions = context.RenderingConventions;
            context.RenderingConventions = new CSharpRenderingConventions(context.Writer);

            using (context.Push(new RuntimeBasicWriter()))
            using (context.Push(new RuntimeTagHelperWriter()))
            {
                using (context.Writer.BuildAsyncLambda(endLine: false))
                {
                    context.RenderChildren(node);
                }
            }
            context.RenderingConventions = initialRenderingConventions;

            context.Writer.WriteEndMethodInvocation();
        }

        public override void WriteSetTagHelperProperty(CSharpRenderingContext context, SetTagHelperPropertyIRNode node)
        {
            throw new NotImplementedException();
        }

        private static string GetTagHelperVariableName(string tagHelperTypeName) => "__" + tagHelperTypeName.Replace('.', '_');
    }
}
