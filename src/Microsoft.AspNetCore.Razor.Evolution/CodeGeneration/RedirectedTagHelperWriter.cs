// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    internal class RedirectedTagHelperWriter : TagHelperWriter
    {
        private readonly TagHelperWriter _previous;
        private readonly string _textWriter;

        public RedirectedTagHelperWriter(TagHelperWriter previous, string textWriter)
        {
            _previous = previous;
            _textWriter = textWriter;
        }

        public string ExecutionContextVariableName { get; set; } = "__tagHelperExecutionContext";

        public string ExecutionContextOutputPropertyName { get; set; } = "Output";

        public string ExecutionContextSetOutputContentAsyncMethodName { get; set; } = "SetOutputContentAsync";

        public string RunnerVariableName { get; set; } = "__tagHelperRunner";

        public string RunnerRunAsyncMethodName { get; set; } = "RunAsync";

        public string ScopeManagerVariableName { get; set; } = "__tagHelperScopeManager";

        public string ScopeManagerEndMethodName { get; set; } = "End";

        public string TagHelperOutputIsContentModifiedPropertyName { get; set; } = "IsContentModified";

        public string WriteTagHelperOutputMethod { get; set; } = "WriteTo";

        public override void WriteAddTagHelperHtmlAttribute(CSharpRenderingContext context, AddTagHelperHtmlAttributeIRNode node)
        {
            _previous.WriteAddTagHelperHtmlAttribute(context, node);
        }

        public override void WriteCreateTagHelper(CSharpRenderingContext context, CreateTagHelperIRNode node)
        {
            _previous.WriteCreateTagHelper(context, node);
        }

        public override void WriteDeclareTagHelperFields(CSharpRenderingContext context, DeclareTagHelperFieldsIRNode node)
        {
            _previous.WriteDeclareTagHelperFields(context, node);
        }

        public override void WriteExecuteTagHelpers(CSharpRenderingContext context, ExecuteTagHelpersIRNode node)
        {
            if (context.Options.DesignTimeMode)
            {
                _previous.WriteExecuteTagHelpers(context, node);
                return;
            }

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
                .Write(_textWriter)
                .WriteParameterSeparator()
                .Write(tagHelperOutputAccessor)
                .WriteEndMethodInvocation()
                .WriteStartAssignment(ExecutionContextVariableName)
                .WriteInstanceMethodInvocation(
                    ScopeManagerVariableName,
                    ScopeManagerEndMethodName);
        }

        public override void WriteInitializeTagHelperStructure(CSharpRenderingContext context, InitializeTagHelperStructureIRNode node)
        {
            _previous.WriteInitializeTagHelperStructure(context, node);
        }

        public override void WriteSetTagHelperProperty(CSharpRenderingContext context, SetTagHelperPropertyIRNode node)
        {
            _previous.WriteSetTagHelperProperty(context, node);
        }
    }
}
