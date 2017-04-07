// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
            throw new NotImplementedException();
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
