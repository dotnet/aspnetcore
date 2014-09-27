// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

        /// <summary>
        /// Gets or sets the <c>SHA1</c> based checksum for the file whose location is defined by <see cref="SourceFile"/>.
        /// </summary>
        public string Checksum { get; set; }

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
