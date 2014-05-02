// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using Microsoft.AspNet.Razor.Generator.Compiler;

namespace Microsoft.AspNet.Razor.Generator
{
    public class CodeGeneratorContext
    {
        private CodeGeneratorContext()
        {
            ExpressionRenderingMode = ExpressionRenderingMode.WriteToOutput;
        }

        // Internal/Private state. Technically consumers might want to use some of these but they can implement them independently if necessary.
        // It's way safer to make them internal for now, especially with the code generator stuff in a bit of flux.
        internal ExpressionRenderingMode ExpressionRenderingMode { get; set; }
        public string SourceFile { get; internal set; }
        public string RootNamespace { get; private set; }
        public string ClassName { get; private set; }
        public RazorEngineHost Host { get; private set; }
        public string TargetWriterName { get; set; }

        public CodeTreeBuilder CodeTreeBuilder { get; set; }

        public static CodeGeneratorContext Create(RazorEngineHost host, string className, string rootNamespace, string sourceFile, bool shouldGenerateLinePragmas)
        {
            return new CodeGeneratorContext()
            {
                CodeTreeBuilder = new CodeTreeBuilder(),
                Host = host,
                SourceFile = shouldGenerateLinePragmas ? sourceFile : null,
                RootNamespace = rootNamespace,
                ClassName = className
            };
        }
    }
}
