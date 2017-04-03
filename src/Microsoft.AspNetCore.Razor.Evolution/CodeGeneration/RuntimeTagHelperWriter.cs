// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    public class RuntimeTagHelperWriter : TagHelperWriter
    {
        public string StringValueBufferVariableName { get; set; } = "__tagHelperStringValueBuffer";

        public string ExecutionContextTypeName { get; set; } = "Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext";

        public string ExecutionContextVariableName { get; set; } = "__tagHelperExecutionContext";

        public string RunnerTypeName { get; set; } = "Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner";

        public string RunnerVariableName { get; set; } = "__tagHelperRunner";

        public string ScopeManagerTypeName { get; set; } = "Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager";

        public string ScopeManagerVariableName { get; set; } = "__tagHelperScopeManager";

        public string StartTagHelperWritingScopeMethodName { get; set; } = "StartTagHelperWritingScope";

        public string EndTagHelperWritingScopeMethodName { get; set; } = "EndTagHelperWritingScope";

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
            .Write("private global::")
            .WriteVariableDeclaration(
                ExecutionContextTypeName,
                ExecutionContextVariableName,
                value: null);

            context.Writer
            .Write("private global::")
            .Write(RunnerTypeName)
            .Write(" ")
            .Write(RunnerVariableName)
            .Write(" = new global::")
            .Write(RunnerTypeName)
            .WriteLine("();");

            var backedScopeManageVariableName = "__backed" + ScopeManagerVariableName;
            context.Writer
                .Write("private global::")
                .WriteVariableDeclaration(
                    ScopeManagerTypeName,
                    backedScopeManageVariableName,
                    value: null);

            context.Writer
            .Write("private global::")
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
            throw new NotImplementedException();
        }

        public override void WriteExecuteTagHelpers(CSharpRenderingContext context, ExecuteTagHelpersIRNode node)
        {
            throw new NotImplementedException();
        }

        public override void WriteInitializeTagHelperStructure(CSharpRenderingContext context, InitializeTagHelperStructureIRNode node)
        {
            throw new NotImplementedException();
        }

        public override void WriteSetTagHelperProperty(CSharpRenderingContext context, SetTagHelperPropertyIRNode node)
        {
            throw new NotImplementedException();
        }

        private static string GetTagHelperVariableName(string tagHelperTypeName) => "__" + tagHelperTypeName.Replace('.', '_');
    }
}
