// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class RedirectedRuntimeTagHelperWriter : RuntimeTagHelperWriter
    {
        private readonly string _textWriter;

        public RedirectedRuntimeTagHelperWriter(string textWriter)
        {
            _textWriter = textWriter;
        }

        public override string WriteTagHelperOutputMethod { get; set; } = "WriteTo";

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
                .Write(_textWriter)
                .WriteParameterSeparator()
                .Write(tagHelperOutputAccessor)
                .WriteEndMethodInvocation()
                .WriteStartAssignment(ExecutionContextVariableName)
                .WriteInstanceMethodInvocation(
                    ScopeManagerVariableName,
                    ScopeManagerEndMethodName);
        }
    }
}
