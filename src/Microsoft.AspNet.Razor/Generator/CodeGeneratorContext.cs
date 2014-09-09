// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Generator.Compiler;

namespace Microsoft.AspNet.Razor.Generator
{
    public class CodeGeneratorContext
    {
        protected CodeGeneratorContext(CodeGeneratorContext context)
            : this(context.Host,
                   context.ClassName,
                   context.RootNamespace,
                   context.SourceFile,
                   // True because we're pulling from the provided context's source file.
                   shouldGenerateLinePragmas: true)
        {
            CodeTreeBuilder = context.CodeTreeBuilder;
        }

        public CodeGeneratorContext(RazorEngineHost host,
                                    string className,
                                    string rootNamespace,
                                    string sourceFile,
                                    bool shouldGenerateLinePragmas)
        {
            CodeTreeBuilder = new CodeTreeBuilder();
            Host = host;
            SourceFile = shouldGenerateLinePragmas ? sourceFile : null;
            RootNamespace = rootNamespace;
            ClassName = className;
        }

        public string SourceFile { get; internal set; }

        public string RootNamespace { get; private set; }

        public string ClassName { get; private set; }

        public RazorEngineHost Host { get; private set; }

        public CodeTreeBuilder CodeTreeBuilder { get; set; }
        /// <summary>
        /// Gets or sets the <c>SHA1</c> based checksum for the file whose location is defined by <see cref="SourceFile"/>.
        /// </summary>
        public string Checksum { get; set; }

    }
}
